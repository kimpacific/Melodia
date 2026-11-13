using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>Home page: recommended artists + the genre-filtered Top Chart.</summary>
public partial class HomeViewModel : ViewModelBase
{
    private const int ArtistsPerPage = 4;

    private readonly IMusicCatalog _catalog;
    private readonly TrackFactory _factory;
    private readonly Func<ArtistInfo, Task> _openArtist;
    private List<ArtistViewModel> _artists = new();
    private int _artistPage;
    private bool _chartLoaded;
    private string? _genre;
    private int _version;

    public ObservableCollection<TrackViewModel> ChartTracks { get; } = new();
    public ObservableCollection<ArtistViewModel> VisibleArtists { get; } = new();
    public GenreFilterViewModel Genres { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = "";
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    [ObservableProperty] private bool _showRetry;

    public HomeViewModel(IMusicCatalog catalog, TrackFactory factory, Func<ArtistInfo, Task> openArtist)
    {
        _catalog = catalog;
        _factory = factory;
        _openArtist = openArtist;
        Genres = new GenreFilterViewModel(OnGenreSelectedAsync);
    }

    /// <summary>Called every time Home is shown. Retries automatically if the last load failed.</summary>
    public async Task LoadAsync()
    {
        if (Genres.Cards.Count == 0)
            Genres.SetGenres(GenreFilterService.PopularGenres, GenreFilterService.All);
        if (!_chartLoaded) await LoadChartAsync();
    }

    private Task OnGenreSelectedAsync(string genre)
    {
        _genre = string.Equals(genre, GenreFilterService.All, StringComparison.OrdinalIgnoreCase) ? null : genre;
        return LoadChartAsync();
    }

    [RelayCommand] private Task RetryAsync() => LoadChartAsync();

    private async Task LoadChartAsync()
    {
        var version = ++_version;
        StatusMessage = "Loading recommendations…";
        ShowRetry = false;
        try
        {
            var chart = await _catalog.GetChartAsync(_genre);
            if (version != _version) return;

            ChartTracks.Clear();
            var i = 1;
            foreach (var t in chart.Tracks.Take(10)) ChartTracks.Add(_factory.Create(t, i++, ChartTracks));

            // artists follow the "All" chart; a genre filter only changes the track list
            if (_genre is null || _artists.Count == 0)
            {
                _artists = chart.Artists.Select(a => new ArtistViewModel(a, _openArtist)).ToList();
                _artistPage = 0;
                ShowArtistPage();
            }

            _chartLoaded = true;
            StartupTimer.Mark("Home chart loaded");
            StatusMessage = ChartTracks.Count == 0 ? "No songs found for this genre right now." : "";
        }
        catch (Exception ex)
        {
            if (version != _version) return;
            if (ChartTracks.Count == 0) _chartLoaded = false;
            StatusMessage = $"Couldn't reach Audius ({ex.Message}). Check your internet connection, then try again.";
            ShowRetry = true;
        }
    }

    // ---- artist carousel ----

    [RelayCommand] private void NextArtists() => PageArtists(+1);
    [RelayCommand] private void PreviousArtists() => PageArtists(-1);

    private void PageArtists(int direction)
    {
        if (_artists.Count == 0) return;
        var pages = (int)Math.Ceiling(_artists.Count / (double)ArtistsPerPage);
        _artistPage = ((_artistPage + direction) % pages + pages) % pages;
        ShowArtistPage();
    }

    private void ShowArtistPage()
    {
        VisibleArtists.Clear();
        foreach (var a in _artists.Skip(_artistPage * ArtistsPerPage).Take(ArtistsPerPage)) VisibleArtists.Add(a);
    }
}
