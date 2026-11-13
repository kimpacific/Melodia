using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>
/// One reusable page for every list of songs: Explore, search results, an artist, an album, Songs,
/// Favorites and playlists. Online lists re-query by genre; local lists filter in memory.
/// </summary>
public partial class TrackListViewModel : ViewModelBase
{
    private readonly IMusicCatalog _catalog;
    private readonly GenreFilterService _genres;
    private readonly TrackFactory _factory;
    private readonly LibraryViewModel _library;

    private List<Track> _source = new();
    private Func<string?, Task<List<Track>>>? _remote;
    private Func<List<Track>>? _local;
    private string _emptyMessage = "";
    private bool _isFavoritesPage;
    private bool _canRemove;
    private int _version;

    public ObservableCollection<TrackViewModel> Tracks { get; } = new();
    public GenreFilterViewModel Genres { get; }

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _subtitle = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = "";
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsPlaylistPage))]
    private PlaylistViewModel? _playlist;
    public bool IsPlaylistPage => Playlist != null;

    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private string _renameText = "";

    public TrackListViewModel(IMusicCatalog catalog, GenreFilterService genres, TrackFactory factory, LibraryViewModel library)
    {
        _catalog = catalog;
        _genres = genres;
        _factory = factory;
        _library = library;
        Genres = new GenreFilterViewModel(OnGenreSelectedAsync);
    }

    // ======================= pages =======================

    public Task ShowExploreAsync()
    {
        Begin("Explore", null);
        return LoadRemoteAsync(g => _catalog.GetTrendingTracksAsync(g, 50), "Nothing to show right now.");
    }

    public Task ShowSearchAsync(string query)
    {
        Begin($"Results for “{query}”", null);
        return LoadRemoteAsync(g => _catalog.SearchAsync(query, g), "No results found.");
    }

    public async Task ShowArtistAsync(ArtistInfo artist)
    {
        Begin(artist.Name, null);
        StatusMessage = "Loading songs…";
        var version = _version;
        try
        {
            var tracks = await _catalog.GetArtistTracksAsync(artist.Id);
            if (version != _version) return;
            ShowLocal(() => tracks, "No streamable songs found for this artist.");
        }
        catch (Exception ex)
        {
            if (version == _version) StatusMessage = $"Couldn't load this artist ({ex.Message}).";
        }
    }

    public async Task ShowAlbumAsync(AlbumInfo album)
    {
        Begin(album.Title, null);
        StatusMessage = "Loading songs…";
        var version = _version;
        try
        {
            var tracks = await _catalog.GetAlbumTracksAsync(album.Id);
            if (version != _version) return;
            ShowLocal(() => tracks, "No streamable songs in this album.");
            Subtitle = $"{album.Artist}  ·  {Tracks.Count} songs";
        }
        catch (Exception ex)
        {
            if (version == _version) StatusMessage = $"Couldn't load this album ({ex.Message}).";
        }
    }

    public void ShowSongs()
    {
        Begin("Songs", null);
        ShowLocal(_library.AllSongs, "Your library is empty. Add songs from a playlist, or heart some tracks.");
    }

    public void ShowFavorites()
    {
        Begin("Favorites", null);
        _isFavoritesPage = true;
        ShowLocal(_library.FavoriteTracks, "No favorites yet. Tap the heart on any song.");
    }

    public void ShowPlaylist(PlaylistViewModel playlist)
    {
        if (!_library.Playlists.Contains(playlist))
        {
            Begin(playlist.Name, null);
            StatusMessage = "This playlist no longer exists.";
            return;
        }
        Begin(playlist.Name, playlist);
        ShowLocal(() => playlist.Data.Tracks.ToList(), "This playlist is empty. Use “Add songs”, or the + button in the player bar.", canRemove: true);
    }

    // ======================= loading / filtering =======================

    private void Begin(string title, PlaylistViewModel? playlist)
    {
        _version++;
        Title = title;
        Subtitle = "";
        Playlist = playlist;
        IsRenaming = false;
        foreach (var p in _library.Playlists) p.IsActive = p == playlist;
        Tracks.Clear();
        StatusMessage = "";
        _source = new List<Track>();
        _remote = null;
        _local = null;
        _isFavoritesPage = false;
        _canRemove = false;
        Genres.SetGenres(Array.Empty<string>(), GenreFilterService.All);
    }

    private async Task LoadRemoteAsync(Func<string?, Task<List<Track>>> provider, string emptyMessage)
    {
        _remote = provider;
        _emptyMessage = emptyMessage;
        Genres.SetGenres(GenreFilterService.PopularGenres, GenreFilterService.All);
        await ReloadRemoteAsync(null);
    }

    private async Task ReloadRemoteAsync(string? genre)
    {
        if (_remote is null) return;
        var version = ++_version;
        StatusMessage = "Loading…";
        try
        {
            var tracks = await _remote(genre);
            if (version != _version) return;
            Fill(tracks, genre is null ? _emptyMessage : $"No {genre} songs found.");
        }
        catch (Exception ex)
        {
            if (version == _version) StatusMessage = $"Couldn't load songs ({ex.Message}). Check your connection and try again.";
        }
    }

    private void ShowLocal(Func<List<Track>> provider, string emptyMessage, bool canRemove = false)
    {
        _local = provider;
        _emptyMessage = emptyMessage;
        _canRemove = canRemove;
        _source = provider();
        Genres.SetGenres(_genres.GenresIn(_source), GenreFilterService.All);
        Fill(_source, emptyMessage);
    }

    private Task OnGenreSelectedAsync(string genre)
    {
        if (_remote != null)
            return ReloadRemoteAsync(string.Equals(genre, GenreFilterService.All, StringComparison.OrdinalIgnoreCase) ? null : genre);

        Fill(_genres.Filter(_source, genre), $"No {genre} songs here yet.");
        return Task.CompletedTask;
    }

    private void Fill(IEnumerable<Track> tracks, string emptyMessage)
    {
        Action<TrackViewModel>? remover = _canRemove ? RemoveTrack : null;
        Tracks.Clear();
        var i = 1;
        foreach (var t in tracks) Tracks.Add(_factory.Create(t, i++, Tracks, remover));
        Subtitle = $"{Tracks.Count} songs";
        StatusMessage = Tracks.Count == 0 ? emptyMessage : "";
    }

    private void Renumber()
    {
        for (var i = 0; i < Tracks.Count; i++) Tracks[i].Index = i + 1;
        Subtitle = $"{Tracks.Count} songs";
    }

    // ======================= reacting to library changes =======================

    /// <summary>
    /// Keeps the Favorites page in step with the hearts. Un-favoriting just removes the row, so the genre
    /// filter and scroll position are not reset.
    /// </summary>
    public void OnFavoriteChanged(Track track, bool isFavorite)
    {
        if (!_isFavoritesPage) return;

        if (!isFavorite)
        {
            _source.RemoveAll(t => t.Id == track.Id);
            for (var i = Tracks.Count - 1; i >= 0; i--)
                if (Tracks[i].Model.Id == track.Id) Tracks.RemoveAt(i);
            Renumber();
            if (_source.Count == 0)
            {
                StatusMessage = _emptyMessage;
                Genres.SetGenres(Array.Empty<string>(), GenreFilterService.All);
            }
            return;
        }

        _source = _local?.Invoke() ?? _source;
        var selected = Genres.Selected;
        Genres.SetGenres(_genres.GenresIn(_source), selected);
        Fill(_genres.Filter(_source, selected), _emptyMessage);
    }

    private void RemoveTrack(TrackViewModel row)
    {
        if (Playlist is null) return;
        _library.RemoveTrack(Playlist, row.Model);
        _source.RemoveAll(t => t.Id == row.Model.Id);
        Tracks.Remove(row);
        Renumber();
        if (_source.Count == 0)
        {
            StatusMessage = _emptyMessage;
            Genres.SetGenres(Array.Empty<string>(), GenreFilterService.All);
        }
    }

    // ======================= commands =======================

    [RelayCommand]
    private void PlayAll()
    {
        if (Tracks.Count > 0) Tracks[0].PlayCommand.Execute(null);
    }

    [RelayCommand]
    private async Task AddSongsAsync()
    {
        var playlist = Playlist;
        if (playlist is null) return;
        var files = await FilePicker.PickAudioAsync("Add songs to playlist", multiple: true);
        if (files.Count == 0) return;
        var tracks = await Task.Run(() => files.Select(LocalTrackReader.Read).ToList());
        _library.AddTracks(playlist, tracks);
        ShowPlaylist(playlist);
    }

    [RelayCommand]
    private void DeletePlaylist()
    {
        if (Playlist != null) _library.DeletePlaylist(Playlist);
    }

    [RelayCommand]
    private void StartRename()
    {
        if (Playlist is null) return;
        RenameText = Playlist.Name;
        IsRenaming = true;
    }

    [RelayCommand] private void CancelRename() => IsRenaming = false;

    [RelayCommand]
    private void ConfirmRename()
    {
        var playlist = Playlist;
        IsRenaming = false;
        if (playlist is null) return;
        if (_library.RenamePlaylist(playlist, RenameText)) Title = playlist.Name;
    }
}
