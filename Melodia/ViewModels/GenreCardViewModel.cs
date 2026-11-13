using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>A genre card: background picture + icon + name. Artwork lives in Assets/Genres/&lt;slug&gt;.jpg.</summary>
public partial class GenreCardViewModel : ViewModelBase
{
    private static readonly ConcurrentDictionary<string, Bitmap?> Images = new();
    private static readonly ConcurrentDictionary<string, Geometry?> Icons = new();

    private readonly Func<string, Task> _onSelect;

    public string Name { get; }
    public Bitmap? Image { get; }
    public Geometry? Icon { get; }

    [ObservableProperty] private bool _isSelected;

    public GenreCardViewModel(string name, bool isSelected, Func<string, Task> onSelect)
    {
        Name = name;
        _isSelected = isSelected;
        _onSelect = onSelect;

        var style = GenreCatalog.Resolve(name);
        Image = Images.GetOrAdd(style.Slug, LoadImage);
        Icon = Icons.GetOrAdd(style.IconPath, LoadIcon);
    }

    private static Bitmap? LoadImage(string slug)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri($"avares://Melodia/Assets/Genres/{slug}.jpg"));
            return new Bitmap(stream);
        }
        catch
        {
            return null;   // the card still shows its gradient overlay and icon
        }
    }

    private static Geometry? LoadIcon(string path)
    {
        try { return Geometry.Parse(path); }
        catch { return null; }
    }

    [RelayCommand] private Task Select() => _onSelect(Name);
}
