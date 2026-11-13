using Melodia.Services;
using Xunit;

namespace Melodia.Tests;

public class NavigationHistoryTests
{
    [Fact]
    public async Task Back_and_forward_replay_pages_in_order()
    {
        var shown = new List<string>();
        var h = new NavigationHistory();
        await h.NavigateAsync(() => { shown.Add("home"); return Task.CompletedTask; });
        await h.NavigateAsync(() => { shown.Add("explore"); return Task.CompletedTask; });
        Assert.True(h.CanGoBack);
        Assert.False(h.CanGoForward);

        await h.BackAsync();
        await h.ForwardAsync();
        Assert.Equal(new[] { "home", "explore", "home", "explore" }, shown);
    }

    [Fact]
    public async Task A_new_page_discards_forward_history()
    {
        var h = new NavigationHistory();
        await h.NavigateAsync(() => Task.CompletedTask);
        await h.NavigateAsync(() => Task.CompletedTask);
        await h.BackAsync();
        Assert.True(h.CanGoForward);
        await h.NavigateAsync(() => Task.CompletedTask);
        Assert.False(h.CanGoForward);
        Assert.Equal(2, h.Count);
    }

    [Fact]
    public async Task Back_on_the_first_page_does_nothing()
    {
        var h = new NavigationHistory();
        await h.NavigateAsync(() => Task.CompletedTask);
        await h.BackAsync();
        Assert.False(h.CanGoBack);
    }
}
