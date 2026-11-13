using System.Net.Http.Headers;
using System.Text.Json;
using Melodia.Models;

namespace Melodia.Services;

/// <summary>
/// Audius public API (https://docs.audius.org). No key needed; tracks stream in full.
/// Optional: set the AUDIUS_BEARER_TOKEN environment variable for higher rate limits.
/// Please keep artist names visible in the UI (Audius asks apps to credit artists).
/// </summary>
public sealed class AudiusService : IMusicCatalog
{
    private const string AppName = "Melodia";
    private const string StreamBase = "https://api.audius.co/v1";

    // Tried in order when the previous host can't be reached (network error, timeout or 5xx).
    private static readonly string[] ApiBases =
    {
        "https://api.audius.co/v1",
        "https://discoveryprovider.audius.co/v1",
        "https://discoveryprovider2.audius.co/v1",
    };

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var token = Environment.GetEnvironmentVariable("AUDIUS_BEARER_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ---------------- tracks ----------------

    /// <summary>Trending tracks (optionally for one genre) plus the artists behind them.</summary>
    public async Task<ChartResult> GetChartAsync(string? genre, int limit = 50)
    {
        var items = await FetchTrendingAsync(genre, "week");
        if (items.Count == 0 && genre != null) items = await FetchTrendingAsync(genre, "allTime");
        return new ChartResult(ParseTracks(items).Take(limit).ToList(), ExtractArtists(items, 12));
    }

    public async Task<List<Track>> GetTrendingTracksAsync(string? genre, int limit = 50)
        => (await GetChartAsync(genre, limit)).Tracks;

    public async Task<List<Track>> SearchAsync(string query, string? genre = null)
    {
        using var doc = await GetJsonAsync($"tracks/search?query={Uri.EscapeDataString(query)}&limit=40{GenreParam(genre)}");
        return ParseTracks(Items(doc.RootElement)).ToList();
    }

    public async Task<List<Track>> GetArtistTracksAsync(string artistId)
    {
        using var doc = await GetJsonAsync($"users/{Uri.EscapeDataString(artistId)}/tracks?limit=40");
        return ParseTracks(Items(doc.RootElement)).ToList();
    }

    // ---------------- artists ----------------

    public async Task<List<ArtistInfo>> SearchArtistsAsync(string query)
    {
        using var doc = await GetJsonAsync($"users/search?query={Uri.EscapeDataString(query)}&limit=24");
        return Items(doc.RootElement).Select(ParseUser).Where(a => a != null).Select(a => a!).ToList();
    }

    /// <summary>Artists behind this week's trending tracks.</summary>
    public async Task<List<ArtistInfo>> GetTopArtistsAsync()
    {
        var items = await FetchTrendingAsync(null, "week");
        return ExtractArtists(items, 30);
    }

    // ---------------- albums ----------------

    /// <summary>Trending albums (Audius albums are playlists flagged is_album). Falls back to playlists.</summary>
    public async Task<List<AlbumInfo>> GetTrendingAlbumsAsync(int take = 15)
    {
        var albums = await FetchPlaylistsAsync("album", take);
        if (albums.Count == 0) albums = await FetchPlaylistsAsync("playlist", take);
        return albums;
    }

    public async Task<List<Track>> GetAlbumTracksAsync(string albumId)
    {
        using var doc = await GetJsonAsync($"playlists/{Uri.EscapeDataString(albumId)}/tracks");
        return ParseTracks(Items(doc.RootElement)).ToList();
    }

    private async Task<List<AlbumInfo>> FetchPlaylistsAsync(string type, int take)
    {
        using var doc = await GetJsonAsync($"playlists/trending?time=week&type={type}");
        return Items(doc.RootElement).Select(ParsePlaylist).Where(a => a != null).Select(a => a!).Take(take).ToList();
    }

    // ---------------- http + json helpers ----------------

    private async Task<List<JsonElement>> FetchTrendingAsync(string? genre, string time)
    {
        using var doc = await GetJsonAsync($"tracks/trending?time={time}&limit=50{GenreParam(genre)}");
        return Items(doc.RootElement);   // Items() clones, so the list stays valid after the document is disposed
    }

    private static string GenreParam(string? genre) =>
        string.IsNullOrWhiteSpace(genre) ? "" : "&genre=" + Uri.EscapeDataString(genre);

    private static async Task<JsonDocument> GetJsonAsync(string path)
    {
        Exception? last = null;
        foreach (var baseUrl in ApiBases)
        {
            var url = $"{baseUrl}/{path}{(path.Contains('?') ? "&" : "?")}app_name={AppName}";
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    using var resp = await Http.GetAsync(url);
                    if ((int)resp.StatusCode >= 500)
                    {
                        last = new HttpRequestException($"Audius answered {(int)resp.StatusCode}.");
                        await Task.Delay(300);
                        continue;
                    }
                    resp.EnsureSuccessStatusCode();   // 4xx: a real error, don't try other hosts
                    await using var stream = await resp.Content.ReadAsStreamAsync();
                    return await JsonDocument.ParseAsync(stream);
                }
                catch (HttpRequestException ex) when (ex.StatusCode is null)
                {
                    last = ex;                         // DNS / connection problem
                    await Task.Delay(300);
                }
                catch (TaskCanceledException ex)
                {
                    last = ex;                         // timeout
                }
            }
        }
        throw last ?? new HttpRequestException("Audius could not be reached.");
    }

    /// <summary>
    /// The "data" array as standalone elements. Each element is Clone()d so it does NOT depend on the
    /// JsonDocument that owns it: callers dispose the document right away (this was the cause of the
    /// "Cannot access a disposed object. Object name: 'JsonDocument'" error).
    /// </summary>
    private static List<JsonElement> Items(JsonElement root)
    {
        var data = Obj(root, "data");
        return data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray().Select(e => e.Clone()).ToList()
            : new List<JsonElement>();
    }

    private static IEnumerable<Track> ParseTracks(IEnumerable<JsonElement> items) =>
        items.Select(ParseTrack).Where(t => t != null).Select(t => t!);

    private static Track? ParseTrack(JsonElement e)
    {
        var id = Str(e, "id");
        if (id.Length == 0) return null;
        // skip tracks that can't be streamed (removed, or gated behind purchase / followers)
        if (Obj(e, "is_streamable").ValueKind == JsonValueKind.False) return null;
        if (Obj(Obj(e, "access"), "stream").ValueKind == JsonValueKind.False) return null;

        var artwork = Obj(e, "artwork");
        var cover = Str(artwork, "150x150");
        if (cover.Length == 0) cover = Str(artwork, "480x480");

        return new Track
        {
            Id = "au:" + id,
            Title = Str(e, "title"),
            Artist = Str(Obj(e, "user"), "name"),
            Album = Str(Obj(e, "album_backlink"), "playlist_name"),
            Genre = Str(e, "genre"),
            DurationSeconds = (int)Num(e, "duration"),
            Source = $"{StreamBase}/tracks/{id}/stream?app_name={AppName}",
            CoverUrl = cover.Length > 0 ? cover : null,
            IsLocal = false
        };
    }

    private static AlbumInfo? ParsePlaylist(JsonElement e)
    {
        var id = Str(e, "id");
        if (id.Length == 0) return null;
        var count = (int)Num(e, "track_count");
        if (count == 0) return null;

        var artwork = Obj(e, "artwork");
        var cover = Str(artwork, "480x480");
        if (cover.Length == 0) cover = Str(artwork, "150x150");

        var date = Str(e, "release_date");
        if (date.Length == 0) date = Str(e, "created_at");
        var year = date.Length >= 4 && char.IsDigit(date[0]) ? date[..4] : "";

        return new AlbumInfo(id, Str(e, "playlist_name"), Str(Obj(e, "user"), "name"), cover, count, year);
    }

    private static ArtistInfo? ParseUser(JsonElement u)
    {
        var id = Str(u, "id");
        if (id.Length == 0) return null;

        var trackCount = (int)Num(u, "track_count");
        if (Obj(u, "track_count").ValueKind == JsonValueKind.Number && trackCount == 0) return null; // nothing to play

        var name = Str(u, "name");
        if (name.Length == 0) name = Str(u, "handle");

        var pic = Str(Obj(u, "profile_picture"), "480x480");
        if (pic.Length == 0) pic = Str(Obj(u, "profile_picture"), "150x150");

        return new ArtistInfo(id, name, pic, (int)Num(u, "follower_count"), trackCount);
    }

    private static List<ArtistInfo> ExtractArtists(IEnumerable<JsonElement> items, int max)
    {
        var seen = new HashSet<string>();
        var list = new List<ArtistInfo>();
        foreach (var e in items)
        {
            var user = Obj(e, "user");
            var id = Str(user, "id");
            if (id.Length == 0 || !seen.Add(id)) continue;

            var pic = Str(Obj(user, "profile_picture"), "480x480");
            if (pic.Length == 0) pic = Str(Obj(user, "profile_picture"), "150x150");
            if (pic.Length == 0) pic = Str(Obj(e, "artwork"), "480x480");

            list.Add(new ArtistInfo(id, Str(user, "name"), pic, (int)Num(user, "follower_count"), (int)Num(user, "track_count")));
            if (list.Count >= max) break;
        }
        return list;
    }

    private static JsonElement Obj(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : default;

    private static string Str(JsonElement e, string name)
    {
        var v = Obj(e, name);
        return v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    }

    private static long Num(JsonElement e, string name)
    {
        var v = Obj(e, name);
        return v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) ? n : 0;
    }
}
