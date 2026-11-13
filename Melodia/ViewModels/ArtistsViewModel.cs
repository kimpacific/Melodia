using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>
/// The Artists tab. Type an artist's name to find them, then open an artist to see and play their songs.
/// Before searching it shows this week's popular artists.
/// </summary>
public partial class ArtistsViewModel : ViewModelBase
{
    private readonly IMusicCatalog _catalog;
    private readonly Func<ArtistInfo, Task> _open;
    private int _version;

    public ObservableCollection<ArtistViewModel> Artists { get; } = new();

    [ObservableProperty] private string _query = "";
    [ObservableProperty] private string _heading = "Popular artists";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = "";
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public ArtistsViewModel(IMusicCatalog catalog, Func<ArtistInfo, Task> open)
    {
        _catalog = catalog;
        _open = open;
    }

    /// <summary>Shown each time the tab opens; keeps earlier search results if there are any.</summary>
    public async Task LoadAsync()
    {
        if (Artists.Count == 0) await ShowPopularAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var query = Query.Trim();
        if (query.Length == 0)
        {
            await ShowPopularAsync();
            return;
        }

        var version = ++_version;
        StatusMessage = $"Searching for “{query}”…";
        try
        {
            var found = await _catalog.SearchArtistsAsync(query);
            if (version != _version) return;

            Heading = $"Artists matching “{query}”";
            Fill(found);
            StatusMessage = found.Count == 0 ? $"No artists found for “{query}”. Try another spelling." : "";

            // one clear match: go straight to their songs
            if (found.Count > 0 && found[0].Name.Equals(query, StringComparison.OrdinalIgnoreCase)
                && (found.Count == 1 || !found[1].Name.Equals(query, StringComparison.OrdinalIgnoreCase)))
                await _open(found[0]);
        }
        catch (Exception ex)
        {
            if (version == _version) StatusMessage = $"Couldn't search Audius ({ex.Message}). Check your connection and try again.";
        }
    }

    private async Task ShowPopularAsync()
    {
        var version = ++_version;
        StatusMessage = "Loading popular artists…";
        try
        {
            var top = await _catalog.GetTopArtistsAsync();
            if (version != _version) return;
            Heading = "Popular artists";
            Fill(top);
            StatusMessage = top.Count == 0 ? "No artists to show right now." : "";
        }
        catch (Exception ex)
        {
            if (version == _version) StatusMessage = $"Couldn't load artists ({ex.Message}). Press Search to retry.";
        }
    }

    private void Fill(IEnumerable<ArtistInfo> artists)
    {
        Artists.Clear();
        foreach (var a in artists) Artists.Add(new ArtistViewModel(a, _open));
    }
}
