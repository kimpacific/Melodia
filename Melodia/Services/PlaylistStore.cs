using System.Text.Json;
using Melodia.Models;

namespace Melodia.Services;

/// <summary>
/// Saves playlists + favorites + equalizer settings as JSON in the user's app-data folder.
/// Writes are atomic (temp file, then replace, keeping a .bak) and happen on a background thread, so a crash
/// mid-save can't corrupt the library and saving never blocks the UI. A corrupt file is kept as .corrupt
/// instead of being silently overwritten, and the .bak is used as a fallback.
/// </summary>
public sealed class PlaylistStore : ILibraryStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private Task _pending = Task.CompletedTask;

    public PlaylistStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Melodia", "library.json");
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    public AppData Load()
    {
        var data = TryRead(_path) ?? TryRead(_path + ".bak");
        if (data is null)
        {
            if (File.Exists(_path))
            {
                try { File.Copy(_path, _path + ".corrupt", true); } catch { /* best effort */ }
            }
            data = new AppData();
            data.Playlists.Add(new PlaylistData { Name = "Study" });
            data.Playlists.Add(new PlaylistData { Name = "Relax" });
            return data;
        }

        data.Playlists ??= new();
        data.Favorites ??= new();
        data.Equalizer ??= new();
        // drop old 30-second preview tracks saved by the first version (Deezer)
        foreach (var p in data.Playlists) p.Tracks.RemoveAll(t => t.Id.StartsWith("dz:"));
        data.Favorites.RemoveAll(t => t.Id.StartsWith("dz:"));
        return data;
    }

    private static AppData? TryRead(string file)
    {
        try
        {
            return File.Exists(file) ? JsonSerializer.Deserialize(File.ReadAllText(file), LibraryJson.Default.AppData) : null;
        }
        catch
        {
            return null;
        }
    }

    public void Save(AppData data)
    {
        string json;
        try { json = JsonSerializer.Serialize(data, LibraryJson.Default.AppData); }   // snapshot on the caller's thread
        catch { return; }

        lock (_gate)
            _pending = _pending.ContinueWith(_ => Write(json), TaskScheduler.Default);
    }

    private void Write(string json)
    {
        try
        {
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_path)) File.Replace(tmp, _path, _path + ".bak");
            else File.Move(tmp, _path);
        }
        catch { /* ignore disk errors; the next save will try again */ }
    }

    public void Flush()
    {
        Task pending;
        lock (_gate) pending = _pending;
        try { pending.Wait(TimeSpan.FromSeconds(3)); } catch { /* ignore */ }
    }
}
