using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>The album shown in the side panel, with its tracks.</summary>
public partial class AlbumViewModel : ViewModelBase
{
    public string Title { get; }
    public string Artist { get; }
    public string Info { get; }
    public ObservableCollection<TrackViewModel> Tracks { get; } = new();

    [ObservableProperty] private Bitmap? _cover;

    public AlbumViewModel(AlbumInfo info)
    {
        Title = info.Title;
        Artist = info.Artist;
        Info = string.IsNullOrEmpty(info.Year)
            ? $"{info.TrackCount} Songs"
            : $"{info.Year}  |  {info.TrackCount} Songs";
        _ = LoadAsync(info.CoverUrl);
    }

    private async Task LoadAsync(string url) => Cover = await ImageLoader.LoadAsync(url);
}
