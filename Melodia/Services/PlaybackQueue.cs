using Melodia.Models;

namespace Melodia.Services;

/// <summary>
/// The play queue: ordering, shuffle and repeat, with no UI or audio dependencies (so it can be unit tested).
/// "auto" means the song ended by itself; Next/Previous buttons pass auto = false and always wrap around.
/// </summary>
public sealed class PlaybackQueue<T> where T : class
{
    private readonly Random _rng;
    private List<T> _items = new();
    private List<int> _order = new();   // play order: indexes into _items
    private int _pos = -1;              // position inside _order

    public PlaybackQueue(Random? rng = null) => _rng = rng ?? Random.Shared;

    public bool Shuffle { get; private set; }
    public RepeatMode Repeat { get; set; } = RepeatMode.Off;
    public int Count => _items.Count;
    public T? Current => _pos >= 0 && _pos < _order.Count ? _items[_order[_pos]] : null;
    public bool HasNext => _pos >= 0 && _pos < _order.Count - 1;

    /// <summary>Replaces the queue and starts at <paramref name="start"/> (added if it isn't in the list).</summary>
    public void Load(IEnumerable<T> items, T start)
    {
        _items = items.ToList();
        var index = _items.IndexOf(start);
        if (index < 0)
        {
            _items = new List<T> { start };
            index = 0;
        }
        BuildOrder(index);
    }

    public void SetShuffle(bool on)
    {
        if (Shuffle == on) return;
        Shuffle = on;
        if (_items.Count == 0 || _pos < 0 || _pos >= _order.Count) return;
        var currentIndex = _order[_pos];
        BuildOrder(currentIndex);   // keeps the current song playing, reorders the rest
    }

    public T? MoveNext(bool auto)
    {
        if (_items.Count == 0) return null;
        if (auto && Repeat == RepeatMode.One) return Current;

        if (_pos < _order.Count - 1)
        {
            _pos++;
            return Current;
        }

        if (auto && Repeat == RepeatMode.Off) return null;   // reached the end: stop

        // wrap around (repeat-all, or the Next button)
        if (Shuffle) Reshuffle(_order[_pos]);
        _pos = 0;
        return Current;
    }

    public T? MovePrevious()
    {
        if (_items.Count == 0) return null;
        _pos = _pos > 0 ? _pos - 1 : _order.Count - 1;
        return Current;
    }

    private void BuildOrder(int currentIndex)
    {
        _order = Enumerable.Range(0, _items.Count).ToList();
        if (Shuffle)
        {
            _order.Remove(currentIndex);
            ShuffleInPlace(_order);
            _order.Insert(0, currentIndex);
            _pos = 0;
        }
        else
        {
            _pos = currentIndex;
        }
    }

    private void Reshuffle(int avoidFirst)
    {
        ShuffleInPlace(_order);
        if (_order.Count > 1 && _order[0] == avoidFirst)
            (_order[0], _order[1]) = (_order[1], _order[0]);   // don't repeat the same song back to back
    }

    private void ShuffleInPlace(List<int> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
