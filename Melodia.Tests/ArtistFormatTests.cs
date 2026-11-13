using Melodia.ViewModels;
using Xunit;

namespace Melodia.Tests;

public class ArtistFormatTests
{
    [Theory]
    [InlineData(950, "950")]
    [InlineData(1234, "1.2K")]
    [InlineData(2_500_000, "2.5M")]
    public void Follower_counts_are_abbreviated(int n, string expected) =>
        Assert.Equal(expected, ArtistViewModel.Compact(n));
}
