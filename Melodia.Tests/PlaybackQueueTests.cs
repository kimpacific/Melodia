using Melodia.Models;
using Melodia.Services;
using Xunit;

namespace Melodia.Tests;

public class PlaybackQueueTests
{
    private static (PlaybackQueue<string> Queue, string[] Items) Make(params string[] items)
    {
        var q = new PlaybackQueue<string>(new Random(1));
        q.Load(items, items[0]);
        return (q, items);
    }

    [Fact]
    public void Plays_in_order_and_stops_at_the_end_when_repeat_is_off()
    {
        var (q, _) = Make("a", "b", "c");
        Assert.Equal("b", q.MoveNext(auto: true));
        Assert.Equal("c", q.MoveNext(auto: true));
        Assert.Null(q.MoveNext(auto: true));       // ended
        Assert.Equal("c", q.Current);
    }

    [Fact]
    public void Next_button_wraps_even_when_repeat_is_off()
    {
        var (q, _) = Make("a", "b");
        q.MoveNext(auto: false);
        Assert.Equal("a", q.MoveNext(auto: false));
    }

    [Fact]
    public void Repeat_all_wraps_automatically()
    {
        var (q, _) = Make("a", "b");
        q.Repeat = RepeatMode.All;
        q.MoveNext(auto: true);
        Assert.Equal("a", q.MoveNext(auto: true));
    }

    [Fact]
    public void Repeat_one_replays_the_same_song_when_it_ends()
    {
        var (q, _) = Make("a", "b");
        q.Repeat = RepeatMode.One;
        Assert.Equal("a", q.MoveNext(auto: true));
        Assert.Equal("b", q.MoveNext(auto: false));   // the Next button still moves on
    }

    [Fact]
    public void Previous_goes_back_and_wraps()
    {
        var (q, _) = Make("a", "b", "c");
        Assert.Equal("c", q.MovePrevious());
        Assert.Equal("b", q.MovePrevious());
    }

    [Fact]
    public void Shuffle_keeps_the_current_song_first_and_plays_every_song_once()
    {
        var items = Enumerable.Range(0, 20).Select(i => $"s{i}").ToArray();
        var q = new PlaybackQueue<string>(new Random(7));
        q.Load(items, "s5");
        q.SetShuffle(true);
        Assert.Equal("s5", q.Current);

        var played = new List<string> { q.Current! };
        while (q.MoveNext(auto: true) is { } next) played.Add(next);
        Assert.Equal(20, played.Count);
        Assert.Equal(20, played.Distinct().Count());
    }

    [Fact]
    public void Turning_shuffle_off_keeps_the_current_song_and_restores_order()
    {
        var (q, _) = Make("a", "b", "c", "d");
        q.SetShuffle(true);
        var current = q.Current;
        q.SetShuffle(false);
        Assert.Equal(current, q.Current);
        var index = new[] { "a", "b", "c", "d" }.ToList().IndexOf(current!);
        if (index < 3) Assert.Equal(new[] { "a", "b", "c", "d" }[index + 1], q.MoveNext(auto: true));
    }

    [Fact]
    public void A_start_item_missing_from_the_list_becomes_a_one_song_queue()
    {
        var q = new PlaybackQueue<string>();
        q.Load(new[] { "a", "b" }, "x");
        Assert.Equal(1, q.Count);
        Assert.Equal("x", q.Current);
    }

    [Fact]
    public void HasNext_is_false_on_the_last_song()
    {
        var (q, _) = Make("a", "b");
        Assert.True(q.HasNext);
        q.MoveNext(auto: false);
        Assert.False(q.HasNext);
    }
}
