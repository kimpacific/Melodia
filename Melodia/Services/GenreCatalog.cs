namespace Melodia.Services;

/// <summary>
/// Maps a genre name to the artwork (Assets/Genres/&lt;slug&gt;.jpg) and icon used on its card.
/// Matching is forgiving, like the genre filter: "Hip-Hop" and "Rap" both resolve to Hip-Hop/Rap, and unknown
/// tags from local files fall back to a neutral style.
/// </summary>
public static class GenreCatalog
{
    public readonly record struct Style(string Slug, string IconPath);

    // 24x24 icon paths (Material-style). "F0" = even-odd fill so shapes can have holes.
    private const string IconAll = "M4 8h4V4H4v4zm6 12h4v-4h-4v4zm-6 0h4v-4H4v4zm0-6h4v-4H4v4zm6 0h4v-4h-4v4zm6-10v4h4V4h-4zm-6 4h4V4h-4v4zm6 6h4v-4h-4v4zm0 6h4v-4h-4v4z";
    private const string IconElectronic = "M10 20h4V4h-4v16zm-6 0h4v-8H4v8zM16 9v11h4V9h-4z";
    private const string IconHipHop = "M12 14c1.66 0 2.99-1.34 2.99-3L15 5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm5.3-3c0 3-2.54 5.1-5.3 5.1S6.7 14 6.7 11H5c0 3.41 2.72 6.23 6 6.72V21h2v-3.28c3.28-.48 6-3.3 6-6.72h-1.7z";
    private const string IconPop = "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z";
    private const string IconRock = "M12 2c5 0 8 3 8 8 0 5-5 12-8 12s-8-7-8-12c0-5 3-8 8-8z";
    private const string IconAlternative = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 14.5c-2.49 0-4.5-2.01-4.5-4.5S9.51 7.5 12 7.5s4.5 2.01 4.5 4.5-2.01 4.5-4.5 4.5zm0-5.5c-.55 0-1 .45-1 1s.45 1 1 1 1-.45 1-1-.45-1-1-1z";
    private const string IconRnb = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z";
    private const string IconLoFi = "M10 2c-1.82 0-3.53.5-5 1.35C7.99 5.08 10 8.3 10 12s-2.01 6.92-5 8.65C6.47 21.5 8.18 22 10 22c5.52 0 10-4.48 10-10S15.52 2 10 2z";
    private const string IconAmbient = "M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96z";
    private const string IconJazz = "M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z";
    private const string IconClassical = "F0 M3 3h18v18H3z M6 6h2.5v7H6z M10.75 6h2.5v7h-2.5z M15.5 6h2.5v7h-2.5z";
    private const string IconCountry = "M14 6l-3.75 5 2.85 3.8-1.6 1.2C9.81 13.75 7 10 7 10l-6 8h22L14 6z";
    private const string IconFolk = "M12 2L5 12h4l-3 5h5v5h2v-5h5l-3-5h4z";
    private const string IconMetal = "M7 2v11h3v9l7-12h-4l4-8z";
    private const string IconReggae = "M6.76 4.84l-1.8-1.79-1.41 1.41 1.79 1.79 1.42-1.41zM4 10.5H1v2h3v-2zm9-9.95h-2V3.5h2V.55zm7.45 3.91l-1.41-1.41-1.79 1.79 1.41 1.41 1.79-1.79zm-3.21 13.7l1.79 1.8 1.41-1.41-1.8-1.79-1.4 1.4zM20 10.5v2h3v-2h-3zm-8-5c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6-2.69-6-6-6zm-1 16.95h2V19.5h-2v2.95zm-7.45-3.91l1.41 1.41 1.79-1.8-1.41-1.41-1.79 1.8z";
    private const string IconLatin = "M12 2c1 4 5 6 5 11a5 5 0 0 1-10 0c0-2 1-3.5 2-4.5 0 2 1 3 2 3 0-4-1-6 1-9.5z";
    private const string IconWorld = "F0 M12 2a10 10 0 1 0 0 20a10 10 0 1 0 0-20z M12 4a8 8 0 1 1 0 16a8 8 0 1 1 0-16z M11.2 4h1.6v7.2h-1.6z M11.2 12.8h1.6V20h-1.6z M4 11.2h16v1.6H4z";
    private const string IconTrap = "M12 2l10 10-10 10L2 12z";
    private const string IconHouse = "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z";
    private const string IconDefault = "M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z";

    // key = letters/digits only, lower-case (see GenreFilterService.Key)
    private static readonly Dictionary<string, Style> Styles = new()
    {
        ["all"] = new("all", IconAll),
        ["electronic"] = new("electronic", IconElectronic),
        ["hiphoprap"] = new("hiphop", IconHipHop),
        ["pop"] = new("pop", IconPop),
        ["rock"] = new("rock", IconRock),
        ["alternative"] = new("alternative", IconAlternative),
        ["rbsoul"] = new("rnb", IconRnb),
        ["lofi"] = new("lofi", IconLoFi),
        ["ambient"] = new("ambient", IconAmbient),
        ["jazz"] = new("jazz", IconJazz),
        ["classical"] = new("classical", IconClassical),
        ["country"] = new("country", IconCountry),
        ["folk"] = new("folk", IconFolk),
        ["metal"] = new("metal", IconMetal),
        ["reggae"] = new("reggae", IconReggae),
        ["latin"] = new("latin", IconLatin),
        ["world"] = new("world", IconWorld),
        ["trap"] = new("trap", IconTrap),
        ["house"] = new("house", IconHouse),
    };

    public static readonly Style Default = new("default", IconDefault);

    // common tags that don't share a prefix with the Audius genre names
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["rap"] = "hiphoprap", ["rnb"] = "rbsoul", ["soul"] = "rbsoul", ["edm"] = "electronic", ["dance"] = "electronic",
        ["techno"] = "electronic", ["indie"] = "alternative", ["punk"] = "rock", ["blues"] = "jazz",
        ["acoustic"] = "folk", ["singersongwriter"] = "folk", ["chill"] = "lofi", ["downtempo"] = "ambient",
    };

    public static Style Resolve(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre)) return Default;
        if (string.Equals(genre, GenreFilterService.All, StringComparison.OrdinalIgnoreCase)) return Styles["all"];

        var key = GenreFilterService.Key(genre);
        if (key.Length == 0) return Default;
        if (Aliases.TryGetValue(key, out var alias)) key = alias;
        if (Styles.TryGetValue(key, out var exact)) return exact;

        foreach (var (name, style) in Styles)
            if (name != "all" && GenreFilterService.Matches(key, name)) return style;
        return Default;
    }

    /// <summary>Every slug that has an image in Assets/Genres (used by tests to catch a missing file).</summary>
    public static IEnumerable<string> AllSlugs => Styles.Values.Select(s => s.Slug).Append(Default.Slug).Distinct();
}
