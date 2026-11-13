using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>A cover card on the Albums page. Clicking opens the album's songs.</summary>
public partial class AlbumCardViewModel : ViewModelBase
{
    private readonly Func<AlbumInfo, Task> _open;
    public AlbumInfo Info { get; }
    public string Title => Info.Title;
    public string Artist => Info.Artist;
    public string Details => string.IsNullOrEmpty(Info.Year) ? $"{Info.TrackCount} songs" : $"{Info.Year} · {Info.TrackCount} songs";

    [ObservableProperty] private Bitmap? _cover;

    public AlbumCardViewModel(AlbumInfo info, Func<AlbumInfo, Task> open)
    {
        Info = info;
        _open = open;
        _ = LoadAsync();
    }

    private async Task LoadAsync() => Cover = await ImageLoader.LoadAsync(Info.CoverUrl);

    [RelayCommand] private Task Open() => _open(Info);
}
