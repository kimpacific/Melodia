using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>Playlists, favorites and the persisted library (including equalizer settings).</summary>
public partial class LibraryViewModel : ViewModelBase
{
    private readonly ILibraryStore _store;
    private readonly Func<Track?> _currentTrack;

    public AppData Data { get; }
    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();

    public event Action<Track, bool>? FavoriteChanged;
    /// <summary>A playlist was created (or imported). The string is an optional notice for the user.</summary>
    public event Action<PlaylistViewModel, string?>? PlaylistCreated;
    public event Action<PlaylistViewModel>? PlaylistDeleted;
    public event Action<PlaylistViewModel>? PlaylistChanged;
    public event Action<PlaylistViewModel>? PlaylistOpenRequested;
    public event Action<string>? Failed;

    [ObservableProperty] private bool _isCreatingPlaylist;
    [ObservableProperty] private string _newPlaylistName = "";
    [ObservableProperty] private bool _isPlaylistPickerOpen;

    public LibraryViewModel(ILibraryStore store, Func<Track?> currentTrack)
    {
        _store = store;
        _currentTrack = currentTrack;
        Data = store.Load();
        foreach (var p in Data.Playlists) Playlists.Add(MakePlaylist(p));
    }

    private PlaylistViewModel MakePlaylist(PlaylistData data) =>
        new(data, p => PlaylistOpenRequested?.Invoke(p), AddCurrentTo);

    // ======================= favorites =======================

    public bool IsFavorite(string id) => Data.Favorites.Any(f => f.Id == id);

    public void ToggleFavorite(Track track)
    {
        var existing = Data.Favorites.FirstOrDefault(f => f.Id == track.Id);
        if (existing != null) Data.Favorites.Remove(existing);
        else Data.Favorites.Add(track);
        _store.Save(Data);
        FavoriteChanged?.Invoke(track, existing == null);
    }

    public List<Track> FavoriteTracks() => Data.Favorites.ToList();

    /// <summary>Every song in any playlist or favorites, without duplicates.</summary>
    public List<Track> AllSongs() =>
        Data.Playlists.SelectMany(p => p.Tracks).Concat(Data.Favorites)
            .GroupBy(t => t.Id).Select(g => g.First()).ToList();

    // ======================= playlists =======================

    public PlaylistViewModel CreatePlaylist(string name)
    {
        var data = new PlaylistData { Name = UniqueName(name, null) };
        Data.Playlists.Add(data);
        var vm = MakePlaylist(data);
        Playlists.Add(vm);
        _store.Save(Data);
        return vm;
    }

    /// <summary>Renames a playlist (adding "(2)" etc. if the name is taken). Returns false for an empty name.</summary>
    public bool RenamePlaylist(PlaylistViewModel playlist, string name)
    {
        name = name.Trim();
        if (name.Length == 0) return false;
        if (!string.Equals(name, playlist.Name, StringComparison.Ordinal))
            playlist.Rename(UniqueName(name, playlist));
        _store.Save(Data);
        return true;
    }

    private string UniqueName(string name, PlaylistViewModel? ignore)
    {
        var unique = name;
        var n = 2;
        while (Playlists.Any(p => p != ignore && p.Name.Equals(unique, StringComparison.OrdinalIgnoreCase)))
            unique = $"{name} ({n++})";
        return unique;
    }

    public void DeletePlaylist(PlaylistViewModel playlist)
    {
        Data.Playlists.Remove(playlist.Data);
        Playlists.Remove(playlist);
        _store.Save(Data);
        PlaylistDeleted?.Invoke(playlist);
    }

    public int AddTracks(PlaylistViewModel playlist, IEnumerable<Track> tracks)
    {
        var added = 0;
        foreach (var t in tracks)
        {
            if (playlist.Data.Tracks.Any(x => x.Id == t.Id)) continue;
            playlist.Data.Tracks.Add(t);
            added++;
        }
        _store.Save(Data);
        return added;
    }

    public void RemoveTrack(PlaylistViewModel playlist, Track track)
    {
        playlist.Data.Tracks.RemoveAll(t => t.Id == track.Id);
        _store.Save(Data);
    }

    public void SaveEqualizer(EqualizerSettings settings)
    {
        Data.Equalizer = settings;
        _store.Save(Data);
    }

    public void Flush() => _store.Flush();

    // ======================= sidebar commands =======================

    [RelayCommand]
    private void StartNewPlaylist()
    {
        NewPlaylistName = "";
        IsCreatingPlaylist = true;
    }

    [RelayCommand] private void CancelNewPlaylist() => IsCreatingPlaylist = false;

    [RelayCommand]
    private void ConfirmNewPlaylist()
    {
        var name = NewPlaylistName.Trim();
        IsCreatingPlaylist = false;
        if (name.Length == 0) return;
        PlaylistCreated?.Invoke(CreatePlaylist(name), null);
    }

    [RelayCommand]
    private async Task ImportPlaylistAsync()
    {
        var files = await FilePicker.PickAsync("Import playlist (.m3u / .m3u8)", new[] { "*.m3u", "*.m3u8" }, "Playlists", false);
        if (files.Count == 0) return;
        var path = files[0];

        List<Track> tracks;
        try { tracks = await Task.Run(() => LocalTrackReader.ReadM3u(path)); }
        catch (Exception ex) { Failed?.Invoke($"Couldn't read that playlist: {ex.Message}"); return; }

        var name = Path.GetFileNameWithoutExtension(path);
        var vm = CreatePlaylist(string.IsNullOrWhiteSpace(name) ? "Imported playlist" : name);
        AddTracks(vm, tracks);
        PlaylistCreated?.Invoke(vm, tracks.Count == 0 ? "No playable songs were found in that playlist file." : null);
    }

    // ======================= "add current song to…" picker =======================

    [RelayCommand] private void OpenPlaylistPicker() => IsPlaylistPickerOpen = _currentTrack() != null;

    private void AddCurrentTo(PlaylistViewModel playlist)
    {
        IsPlaylistPickerOpen = false;
        var track = _currentTrack();
        if (track is null) return;
        AddTracks(playlist, new[] { track });
        PlaylistChanged?.Invoke(playlist);
    }
}
