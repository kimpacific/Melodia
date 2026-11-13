using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>
/// The shell: owns the child view models, navigation and the search box. Everything else lives in
/// <see cref="PlayerViewModel"/>, <see cref="LibraryViewModel"/>, <see cref="EqualizerViewModel"/> and the page view models.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IAudioPlayer? _audio;
    private readonly NavigationHistory _history = new();

    public LibraryViewModel Library { get; }
    public PlayerViewModel Player { get; }
    public EqualizerViewModel Equalizer { get; }
    public HomeViewModel Home { get; }
    public TrackListViewModel ListPage { get; }
    public ArtistsViewModel ArtistsPage { get; }
    public AlbumsViewModel AlbumsPage { get; }
    public AlbumPanelViewModel AlbumPanel { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowAlbumPanel))]
    private ViewModelBase _currentPage;

    [ObservableProperty,
     NotifyPropertyChangedFor(nameof(IsHomeActive), nameof(IsExploreActive), nameof(IsEqualizerActive),
         nameof(IsSongsActive), nameof(IsFavoritesActive), nameof(IsAlbumsActive), nameof(IsArtistsActive))]
    private string _activeNav = "Home";

    [ObservableProperty] private string _searchText = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasNotice))]
    private string _notice = "";

    public bool HasNotice => !string.IsNullOrEmpty(Notice);
    public bool IsHomeActive => ActiveNav == "Home";
    public bool IsExploreActive => ActiveNav == "Explore";
    public bool IsEqualizerActive => ActiveNav == "Equalizer";
    public bool IsSongsActive => ActiveNav == "Songs";
    public bool IsFavoritesActive => ActiveNav == "Favorite";
    public bool IsAlbumsActive => ActiveNav == "Albums";
    public bool IsArtistsActive => ActiveNav == "Artist";

    /// <summary>The side panel is useful next to Home and song lists, but just gets in the way elsewhere.</summary>
    public bool ShowAlbumPanel => CurrentPage is HomeViewModel or TrackListViewModel;

    public MainWindowViewModel(IMusicCatalog catalog, ILibraryStore store, IAudioPlayer? audio, string audioError)
    {
        _audio = audio;
        var genres = new GenreFilterService();

        Library = new LibraryViewModel(store, () => Player?.Current?.Model);
        Player = new PlayerViewModel(audio, audioError, Library.IsFavorite,
            row => Library.ToggleFavorite(row.Model), () => Home?.ChartTracks.FirstOrDefault());
        var factory = new TrackFactory(Player, Library);

        Equalizer = new EqualizerViewModel(audio, Library.Data.Equalizer, Library.SaveEqualizer);
        Home = new HomeViewModel(catalog, factory, OpenArtistAsync);
        ListPage = new TrackListViewModel(catalog, genres, factory, Library);
        ArtistsPage = new ArtistsViewModel(catalog, OpenArtistAsync);
        AlbumsPage = new AlbumsViewModel(catalog, OpenAlbumAsync);
        AlbumPanel = new AlbumPanelViewModel(catalog, factory);
        _currentPage = Home;

        Player.CurrentChanged += RefreshCurrentMarkers;
        Library.FavoriteChanged += OnFavoriteChanged;
        Library.PlaylistOpenRequested += p => _ = OpenPlaylistAsync(p);
        Library.PlaylistCreated += async (p, notice) =>
        {
            await OpenPlaylistAsync(p);
            if (notice != null) Notice = notice;
        };
        Library.PlaylistDeleted += deleted => _ = NavigateAsync(ShowHomeAsync);
        Library.PlaylistChanged += p =>
        {
            if (CurrentPage == ListPage && ListPage.Playlist == p) ListPage.ShowPlaylist(p);
        };
        Library.Failed += message => Notice = message;
        _history.Changed += () =>
        {
            GoBackCommand.NotifyCanExecuteChanged();
            GoForwardCommand.NotifyCanExecuteChanged();
        };
    }

    /// <summary>Loads the first page. Called after the window is on screen so startup feels instant.</summary>
    public void Start()
    {
        _ = AlbumPanel.EnsureLoadedAsync();   // in parallel with the chart
        _ = NavigateAsync(ShowHomeAsync);
    }

    // ======================= navigation =======================

    private Task NavigateAsync(Func<Task> page)
    {
        Notice = "";
        return _history.NavigateAsync(page);
    }

    private void Show(ViewModelBase page, string nav)
    {
        CurrentPage = page;
        ActiveNav = nav;
        foreach (var p in Library.Playlists) p.IsActive = false;
    }

    private async Task ShowHomeAsync()
    {
        Show(Home, "Home");
        await Home.LoadAsync();
        await AlbumPanel.EnsureLoadedAsync();
    }

    private Task ShowExploreAsync() { Show(ListPage, "Explore"); return ListPage.ShowExploreAsync(); }
    private Task ShowSearchAsync(string query) { Show(ListPage, ""); return ListPage.ShowSearchAsync(query); }
    private Task ShowSongsAsync() { Show(ListPage, "Songs"); ListPage.ShowSongs(); return Task.CompletedTask; }
    private Task ShowFavoritesAsync() { Show(ListPage, "Favorite"); ListPage.ShowFavorites(); return Task.CompletedTask; }
    private Task ShowEqualizerAsync() { Show(Equalizer, "Equalizer"); return Task.CompletedTask; }
    private async Task ShowArtistsAsync() { Show(ArtistsPage, "Artist"); await ArtistsPage.LoadAsync(); }
    private async Task ShowAlbumsAsync() { Show(AlbumsPage, "Albums"); await AlbumsPage.LoadAsync(); }

    public Task OpenArtistAsync(ArtistInfo artist) =>
        NavigateAsync(() => { Show(ListPage, ""); return ListPage.ShowArtistAsync(artist); });

    public Task OpenAlbumAsync(AlbumInfo album) =>
        NavigateAsync(() => { Show(ListPage, ""); return ListPage.ShowAlbumAsync(album); });

    public Task OpenPlaylistAsync(PlaylistViewModel playlist) =>
        NavigateAsync(() => { Show(ListPage, "Playlist"); ListPage.ShowPlaylist(playlist); return Task.CompletedTask; });

    private bool CanGoBack() => _history.CanGoBack;
    private bool CanGoForward() => _history.CanGoForward;

    [RelayCommand(CanExecute = nameof(CanGoBack))] private Task GoBackAsync() => _history.BackAsync();
    [RelayCommand(CanExecute = nameof(CanGoForward))] private Task GoForwardAsync() => _history.ForwardAsync();

    [RelayCommand] private Task GoHomeAsync() => NavigateAsync(ShowHomeAsync);
    [RelayCommand] private Task ShowExploreNavAsync() => NavigateAsync(ShowExploreAsync);
    [RelayCommand] private Task ShowSongsNavAsync() => NavigateAsync(ShowSongsAsync);
    [RelayCommand] private Task ShowFavoritesNavAsync() => NavigateAsync(ShowFavoritesAsync);
    [RelayCommand] private Task ShowEqualizerNavAsync() => NavigateAsync(ShowEqualizerAsync);
    [RelayCommand] private Task ShowArtistsNavAsync() => NavigateAsync(ShowArtistsAsync);
    [RelayCommand] private Task ShowAlbumsNavAsync() => NavigateAsync(ShowAlbumsAsync);

    [RelayCommand]
    private Task SearchAsync()
    {
        var query = SearchText.Trim();
        return query.Length == 0 ? Task.CompletedTask : NavigateAsync(() => ShowSearchAsync(query));
    }

    // ======================= keeping rows in sync =======================

    private IEnumerable<TrackViewModel> AllTrackRows() =>
        Home.ChartTracks.Concat(ListPage.Tracks)
            .Concat(AlbumPanel.CurrentAlbum?.Tracks ?? Enumerable.Empty<TrackViewModel>());

    private void RefreshCurrentMarkers()
    {
        var id = Player.Current?.Model.Id;
        foreach (var row in AllTrackRows()) row.IsCurrent = id != null && row.Model.Id == id;
    }

    private void OnFavoriteChanged(Track track, bool isFavorite)
    {
        foreach (var row in AllTrackRows().Where(r => r.Model.Id == track.Id)) row.IsFavorite = isFavorite;
        Player.RefreshFavorite();
        ListPage.OnFavoriteChanged(track, isFavorite);
    }

    // ======================= shutdown =======================

    public void Dispose()
    {
        Equalizer.Flush();
        Player.Dispose();
        Library.Flush();
        _audio?.Dispose();
    }
}
