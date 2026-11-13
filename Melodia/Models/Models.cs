namespace Melodia.Models;

public class Track
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public string Genre { get; set; } = "";
    public int DurationSeconds { get; set; }
    /// <summary>Local file path or http(s) stream URL.</summary>
    public string Source { get; set; } = "";
    public string? CoverUrl { get; set; }
    public bool IsLocal { get; set; }
}

public class PlaylistData
{
    public string Name { get; set; } = "";
    public List<Track> Tracks { get; set; } = new();
}

public class EqualizerSettings
{
    public bool Enabled { get; set; } = true;
    public string Preset { get; set; } = "Flat";
    public double Preamp { get; set; }
    public List<double> Gains { get; set; } = new();
}

public class AppData
{
    public List<PlaylistData> Playlists { get; set; } = new();
    public List<Track> Favorites { get; set; } = new();
    public EqualizerSettings Equalizer { get; set; } = new();
}

public enum RepeatMode { Off, All, One }

public record ArtistInfo(string Id, string Name, string PictureUrl, int Followers = 0, int TrackCount = 0);
public record AlbumInfo(string Id, string Title, string Artist, string CoverUrl, int TrackCount, string Year);
public record ChartResult(List<Track> Tracks, List<ArtistInfo> Artists);
