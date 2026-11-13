using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>The Albums tab: a grid of trending albums. Clicking one opens its songs.</summary>
public partial class AlbumsViewModel : ViewModelBase
{
    private readonly IMusicCatalog _catalog;
    private readonly Func<AlbumInfo, Task> _open;

    public ObservableCollection<AlbumCardViewModel> Albums { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = "";
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public AlbumsViewModel(IMusicCatalog catalog, Func<AlbumInfo, Task> open)
    {
        _catalog = catalog;
        _open = open;
    }

    public async Task LoadAsync()
    {
        if (Albums.Count == 0) await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        StatusMessage = "Loading albums…";
        try
        {
            var albums = await _catalog.GetTrendingAlbumsAsync(30);
            Albums.Clear();
            foreach (var a in albums) Albums.Add(new AlbumCardViewModel(a, _open));
            StatusMessage = albums.Count == 0 ? "No albums available right now." : "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't load albums ({ex.Message}). Check your connection and try again.";
        }
    }
}
