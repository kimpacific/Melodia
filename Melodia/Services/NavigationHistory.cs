namespace Melodia.Services;

/// <summary>Back / forward history. Each entry is a function that (re)shows a page.</summary>
public sealed class NavigationHistory
{
    private const int MaxEntries = 100;
    private readonly List<Func<Task>> _pages = new();
    private int _index = -1;

    public event Action? Changed;

    public bool CanGoBack => _index > 0;
    public bool CanGoForward => _index < _pages.Count - 1;
    public int Count => _pages.Count;

    public Task NavigateAsync(Func<Task> page)
    {
        if (_index < _pages.Count - 1)
            _pages.RemoveRange(_index + 1, _pages.Count - _index - 1);   // a new page discards "forward"
        _pages.Add(page);
        if (_pages.Count > MaxEntries) _pages.RemoveAt(0);
        _index = _pages.Count - 1;
        Changed?.Invoke();
        return page();
    }

    public Task BackAsync()
    {
        if (!CanGoBack) return Task.CompletedTask;
        _index--;
        Changed?.Invoke();
        return _pages[_index]();
    }

    public Task ForwardAsync()
    {
        if (!CanGoForward) return Task.CompletedTask;
        _index++;
        Changed?.Invoke();
        return _pages[_index]();
    }
}
