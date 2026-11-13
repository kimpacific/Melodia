using Melodia.Models;
using Melodia.Services;
using Xunit;

namespace Melodia.Tests;

public class GenreTests
{
    [Theory]
    [InlineData("Electronic", "electronic")]
    [InlineData("Hip-Hop/Rap", "hiphop")]
    [InlineData("Hip Hop", "hiphop")]
    [InlineData("Rap", "hiphop")]
    [InlineData("R&B/Soul", "rnb")]
    [InlineData("RnB", "rnb")]
    [InlineData("Lo-Fi", "lofi")]
    [InlineData("Electronica", "electronic")]
    [InlineData("All", "all")]
    public void Genres_resolve_to_their_artwork(string genre, string slug) =>
        Assert.Equal(slug, GenreCatalog.Resolve(genre).Slug);

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("Something Nobody Has Heard Of")]
    public void Unknown_genres_get_the_default_style(string genre) =>
        Assert.Equal("default", GenreCatalog.Resolve(genre).Slug);

    [Fact]
    public void Every_slug_has_an_image_file()
    {
        var dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Melodia", "Assets", "Genres"));
        foreach (var slug in GenreCatalog.AllSlugs)
            Assert.True(File.Exists(Path.Combine(dir, slug + ".jpg")), $"missing {slug}.jpg in {dir}");
    }

    [Fact]
    public void Filter_matches_loosely_and_keeps_all_for_All()
    {
        var svc = new GenreFilterService();
        var tracks = new[]
        {
            new Track { Id = "1", Genre = "Hip-Hop/Rap" },
            new Track { Id = "2", Genre = "Electronic" },
            new Track { Id = "3", Genre = "" },
        };
        Assert.Equal(3, svc.Filter(tracks, GenreFilterService.All).Count());
        Assert.Single(svc.Filter(tracks, "Hip-Hop"));
        Assert.Single(svc.Filter(tracks, GenreFilterService.Unknown));
    }
}
