using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>The "Albums" side panel: flip through trending albums and play their tracks.</summary>
public partial class AlbumPanelViewModel : ViewModelBase
{
    private readonly IMusicCatalog _catalog;
    private readonly TrackFactory _factory;
    private List<AlbumInfo> _albums = new();
    private int _index;
    private bool _loading;
    private int _version;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasAlbum))]
    private AlbumViewModel? _currentAlbum;
    public bool HasAlbum => CurrentAlbum != null;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _status = "";
    public bool HasStatus => !string.IsNullOrEmpty(Status);

    public AlbumPanelViewModel(IMusicCatalog catalog, TrackFactory factory)
    {
        _catalog = catalog;
        _factory = factory;
    }

    public async Task EnsureLoadedAsync()
    {
        if (CurrentAlbum == null && !_loading) await LoadAsync();
    }

    [RelayCommand] private Task NextAlbumAsync() => ShowAsync(_index + 1);
    [RelayCommand] private Task PreviousAlbumAsync() => ShowAsync(_index - 1);

    private async Task LoadAsync()
    {
        _loading = true;
        Status = "Loading albums…";
        try
        {
            _albums = await _catalog.GetTrendingAlbumsAsync();
            if (_albums.Count == 0)
            {
                Status = "No albums available right now. Use the arrows to retry.";
                return;
            }
            _loading = false;
            await ShowAsync(0);
        }
        catch (Exception ex)
        {
            Status = $"Couldn't load albums ({ex.Message}). Use the arrows to retry.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ShowAsync(int index)
    {
        if (_albums.Count == 0)
        {
            if (!_loading) await LoadAsync();
            return;
        }

        _index = ((index % _albums.Count) + _albums.Count) % _albums.Count;
        var info = _albums[_index];
        var album = new AlbumViewModel(info);
        CurrentAlbum = album;                 // header shows straight away
        Status = "Loading tracks…";
        var version = ++_version;
        try
        {
            var tracks = await _catalog.GetAlbumTracksAsync(info.Id);
            if (version != _version) return;
            var i = 1;
            foreach (var t in tracks) album.Tracks.Add(_factory.Create(t, i++, album.Tracks));
            Status = album.Tracks.Count == 0 ? "No streamable tracks in this album." : "";
        }
        catch (Exception ex)
        {
            if (version == _version) Status = $"Couldn't load this album's tracks ({ex.Message}).";
        }
    }
}
