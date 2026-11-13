using Melodia.Models;

namespace Melodia.Services;

public static class LocalTrackReader
{
    public static Track Read(string path)
    {
        var track = new Track
        {
            Id = "file:" + path,
            Source = path,
            IsLocal = true,
            Title = Path.GetFileNameWithoutExtension(path),
            Artist = "Unknown artist"
        };
        try
        {
            using var f = TagLib.File.Create(path);
            if (!string.IsNullOrWhiteSpace(f.Tag.Title)) track.Title = f.Tag.Title;
            var artist = f.Tag.FirstPerformer ?? f.Tag.FirstAlbumArtist;
            if (!string.IsNullOrWhiteSpace(artist)) track.Artist = artist;
            track.Album = f.Tag.Album ?? "";
            track.Genre = f.Tag.FirstGenre ?? "";
            track.DurationSeconds = (int)f.Properties.Duration.TotalSeconds;
        }
        catch { /* unreadable tags: keep filename-based defaults */ }
        return track;
    }

    public static byte[]? ReadCover(string path)
    {
        try
        {
            using var f = TagLib.File.Create(path);
            return f.Tag.Pictures.FirstOrDefault()?.Data.Data;
        }
        catch { return null; }
    }

    /// <summary>Parses .m3u / .m3u8 playlists (local paths, file:// URIs and http(s) streams).</summary>
    public static List<Track> ReadM3u(string m3uPath)
    {
        var list = new List<Track>();
        var dir = Path.GetDirectoryName(m3uPath) ?? "";
        string? pendingTitle = null;

        foreach (var raw in File.ReadLines(m3uPath))
        {
            var line = raw.Trim().TrimStart('\uFEFF');
            if (line.Length == 0) continue;

            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                var comma = line.IndexOf(',');
                pendingTitle = comma >= 0 ? line[(comma + 1)..].Trim() : null;
                continue;
            }
            if (line.StartsWith('#')) continue;

            if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new Track
                {
                    Id = "url:" + line,
                    Source = line,
                    Title = string.IsNullOrWhiteSpace(pendingTitle) ? line : pendingTitle!,
                    Artist = "Web stream",
                    IsLocal = false
                });
            }
            else
            {
                var p = line.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    ? new Uri(line).LocalPath
                    : line;
                if (!OperatingSystem.IsWindows()) p = p.Replace('\\', '/');
                if (!Path.IsPathRooted(p)) p = Path.GetFullPath(Path.Combine(dir, p));
                if (File.Exists(p)) list.Add(Read(p));
            }
            pendingTitle = null;
        }
        return list;
    }
}
