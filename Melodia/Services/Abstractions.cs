using Melodia.Models;

namespace Melodia.Services;

/// <summary>Online music catalog (currently backed by Audius). Lets the view models be tested with a fake.</summary>
public interface IMusicCatalog
{
    Task<ChartResult> GetChartAsync(string? genre, int limit = 50);
    Task<List<Track>> GetTrendingTracksAsync(string? genre, int limit = 50);
    Task<List<Track>> SearchAsync(string query, string? genre = null);
    Task<List<Track>> GetArtistTracksAsync(string artistId);
    Task<List<ArtistInfo>> SearchArtistsAsync(string query);
    Task<List<ArtistInfo>> GetTopArtistsAsync();
    Task<List<AlbumInfo>> GetTrendingAlbumsAsync(int take = 15);
    Task<List<Track>> GetAlbumTracksAsync(string albumId);
}

/// <summary>Persists playlists, favorites and equalizer settings.</summary>
public interface ILibraryStore
{
    AppData Load();
    /// <summary>Queues a save; the actual disk write happens off the UI thread.</summary>
    void Save(AppData data);
    /// <summary>Blocks (briefly) until queued saves reach the disk. Call on shutdown.</summary>
    void Flush();
}

/// <summary>Audio engine abstraction (LibVLC in the real app).</summary>
public interface IAudioPlayer : IDisposable
{
    event Action? Ended;
    event Action? Failed;
    long TimeMs { get; }
    long LengthMs { get; }
    void Play(string source);
    void Stop();
    void SetPaused(bool paused);
    void SeekFraction(double fraction);
    void SetVolume(int volume);
    void ApplyEqualizer(bool enabled, double preampDb, IReadOnlyList<double> gainsDb);
}
