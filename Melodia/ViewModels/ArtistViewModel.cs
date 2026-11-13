using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

public partial class ArtistViewModel : ViewModelBase
{
    private readonly Func<ArtistInfo, Task> _open;
    public ArtistInfo Info { get; }
    public string Name => Info.Name;

    public string FollowersText =>
        Info.Followers > 0 ? $"{Compact(Info.Followers)} followers"
        : Info.TrackCount > 0 ? $"{Info.TrackCount} tracks"
        : "";

    [ObservableProperty] private Bitmap? _picture;

    public ArtistViewModel(ArtistInfo info, Func<ArtistInfo, Task> open)
    {
        Info = info;
        _open = open;
        _ = LoadAsync();
    }

    private async Task LoadAsync() => Picture = await ImageLoader.LoadAsync(Info.PictureUrl);

    /// <summary>1234 becomes "1.2K", 2500000 becomes "2.5M".</summary>
    public static string Compact(int n) =>
        n >= 1_000_000 ? $"{n / 1_000_000.0:0.#}M"
        : n >= 1_000 ? $"{n / 1_000.0:0.#}K"
        : n.ToString();

    [RelayCommand] private Task Open() => _open(Info);
}
