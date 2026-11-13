using Melodia.Models;
using Melodia.Services;
using Xunit;

namespace Melodia.Tests;

public class LibraryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "melodia-tests-" + Guid.NewGuid().ToString("N"));
    private string LibraryPath => Path.Combine(_dir, "library.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    [Fact]
    public void First_run_creates_the_default_playlists()
    {
        var data = new PlaylistStore(LibraryPath).Load();
        Assert.Equal(new[] { "Study", "Relax" }, data.Playlists.Select(p => p.Name));
    }

    [Fact]
    public void Saved_data_round_trips()
    {
        var store = new PlaylistStore(LibraryPath);
        var data = store.Load();
        data.Favorites.Add(new Track { Id = "au:1", Title = "Song" });
        data.Equalizer.Preamp = 3;
        store.Save(data);
        store.Flush();

        var loaded = new PlaylistStore(LibraryPath).Load();
        Assert.Single(loaded.Favorites);
        Assert.Equal(3, loaded.Equalizer.Preamp);
    }

    [Fact]
    public void A_damaged_file_is_kept_and_the_backup_is_used()
    {
        var store = new PlaylistStore(LibraryPath);
        var data = store.Load();
        data.Favorites.Add(new Track { Id = "au:1", Title = "First" });
        store.Save(data);
        store.Flush();
        data.Favorites.Add(new Track { Id = "au:2", Title = "Second" });
        store.Save(data);          // second save turns the first file into library.json.bak
        store.Flush();

        File.WriteAllText(LibraryPath, "{ this is not json");
        var loaded = new PlaylistStore(LibraryPath).Load();
        Assert.Single(loaded.Favorites);   // restored from .bak
    }

    [Fact]
    public void A_damaged_file_without_a_backup_is_preserved_as_corrupt()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(LibraryPath, "garbage");
        var loaded = new PlaylistStore(LibraryPath).Load();
        Assert.Equal(2, loaded.Playlists.Count);
        Assert.True(File.Exists(LibraryPath + ".corrupt"));
    }

    [Fact]
    public void Old_preview_tracks_are_dropped()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(LibraryPath, "{\"Playlists\":[],\"Favorites\":[{\"Id\":\"dz:1\"},{\"Id\":\"au:2\"}]}");
        var loaded = new PlaylistStore(LibraryPath).Load();
        Assert.Equal("au:2", Assert.Single(loaded.Favorites).Id);
    }

    [Fact]
    public void M3u_reader_keeps_existing_files_and_streams_and_skips_the_rest()
    {
        Directory.CreateDirectory(_dir);
        var audio = Path.Combine(_dir, "song.mp3");
        File.WriteAllText(audio, "not really audio");
        var m3u = Path.Combine(_dir, "list.m3u");
        File.WriteAllLines(m3u, new[] { "#EXTM3U", "song.mp3", "missing.mp3", "https://example.com/stream.mp3" });

        var tracks = LocalTrackReader.ReadM3u(m3u);
        Assert.Contains(tracks, t => t.Source == audio);
        Assert.DoesNotContain(tracks, t => t.Source.EndsWith("missing.mp3"));
    }
}
