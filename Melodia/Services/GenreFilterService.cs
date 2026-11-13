using Melodia.Models;

namespace Melodia.Services;

/// <summary>Works out which genres a set of songs contains and filters songs by genre.</summary>
public sealed class GenreFilterService
{
    public const string All = "All";
    public const string Unknown = "Unknown";

    /// <summary>Genre names exactly as Audius spells them (used for server-side filtering).</summary>
    public static readonly IReadOnlyList<string> PopularGenres = new[]
    {
        "Electronic", "Hip-Hop/Rap", "Pop", "Rock", "Alternative", "R&B/Soul", "Lo-Fi", "Ambient",
        "Jazz", "Classical", "Country", "Folk", "Metal", "Reggae", "Latin", "World", "Trap", "House"
    };

    public string Normalize(string? genre) => string.IsNullOrWhiteSpace(genre) ? Unknown : genre.Trim();

    /// <summary>Distinct genres present in the given songs (empty tags become "Unknown", listed last).</summary>
    public IReadOnlyList<string> GenresIn(IEnumerable<Track> tracks) =>
        tracks.Select(t => Normalize(t.Genre))
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .OrderBy(g => g == Unknown ? 1 : 0)
              .ThenBy(g => g, StringComparer.OrdinalIgnoreCase)
              .ToList();

    /// <summary>Keeps songs whose genre matches (forgiving: "Hip-Hop" matches "Hip-Hop/Rap", "R&amp;B" matches "R&amp;B/Soul").</summary>
    public IEnumerable<Track> Filter(IEnumerable<Track> tracks, string genre)
    {
        if (string.IsNullOrWhiteSpace(genre) || genre == All) return tracks;
        var wanted = Key(genre);
        return tracks.Where(t => Matches(Key(Normalize(t.Genre)), wanted));
    }

    /// <summary>Letters and digits only, lower-case: "R&amp;B/Soul" becomes "rbsoul".</summary>
    public static string Key(string s) =>
        new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    public static bool Matches(string a, string b) =>
        a == b
        || (a.Length >= 2 && b.StartsWith(a, StringComparison.Ordinal))
        || (b.Length >= 2 && a.StartsWith(b, StringComparison.Ordinal));
}
