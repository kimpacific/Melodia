using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

public partial class TrackViewModel : ViewModelBase
{
    private readonly Action<TrackViewModel> _play;
    private readonly Action<Track> _toggleFavorite;

    public Track Model { get; }
    public Task<Bitmap?> CoverTask { get; }

    /// <summary>The list this row belongs to: playing it queues the rest of that list.</summary>
    public IReadOnlyList<TrackViewModel>? Queue { get; set; }

    /// <summary>Set on playlist pages: shows the remove (x) button.</summary>
    public Action<TrackViewModel>? RemoveHandler { get; set; }
    public bool CanRemove => RemoveHandler != null;

    /// <summary>A local file that no longer exists on disk (moved or deleted).</summary>
    public bool IsMissing { get; }

    public string Title => Model.Title;
    public string Artist => Model.Artist;
    public string DurationText => Model.DurationSeconds > 0
        ? $"{Model.DurationSeconds / 60}:{Model.DurationSeconds % 60:00}"
        : "--:--";

    [ObservableProperty] private int _index;
    [ObservableProperty] private bool _isCurrent;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private Bitmap? _cover;

    public TrackViewModel(Track model, int index, bool isFavorite, bool isCurrent,
                          Action<TrackViewModel> play, Action<Track> toggleFavorite)
    {
        Model = model;
        _play = play;
        _toggleFavorite = toggleFavorite;
        _index = index;
        _isFavorite = isFavorite;
        _isCurrent = isCurrent;
        IsMissing = model.IsLocal && !File.Exists(model.Source);
        CoverTask = LoadCoverAsync();
    }

    private async Task<Bitmap?> LoadCoverAsync()
    {
        Bitmap? bitmap;
        if (Model.IsLocal)
        {
            var bytes = await Task.Run(() => LocalTrackReader.ReadCover(Model.Source));
            bitmap = null;
            if (bytes is { Length: > 0 })
            {
                try { using var ms = new MemoryStream(bytes); bitmap = new Bitmap(ms); } catch { }
            }
        }
        else
        {
            bitmap = await ImageLoader.LoadAsync(Model.CoverUrl);
        }
        Cover = bitmap;
        return bitmap;
    }

    [RelayCommand] private void Play() => _play(this);
    [RelayCommand] private void ToggleFavorite() => _toggleFavorite(Model);
    [RelayCommand] private void Remove() => RemoveHandler?.Invoke(this);
}
