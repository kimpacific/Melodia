using Avalonia.Threading;

namespace Melodia.Services;

/// <summary>
/// Starts the LibVLC audio engine on a background thread so the window can appear immediately
/// (loading LibVLC and its plugins can take seconds). Until the engine is ready, volume and equalizer
/// settings are remembered, and a Play request is held and started the moment it is ready.
/// All members are called on the UI thread.
/// </summary>
public sealed class LazyAudioPlayer : IAudioPlayer
{
    private AudioPlayerService? _inner;
    private string? _error;
    private bool _disposed;
    private string? _pendingSource;
    private int _volume = 70;
    private bool _eqEnabled;
    private double _preamp;
    private double[] _gains = new double[10];

    public event Action? Ended;
    public event Action? Failed;

    public LazyAudioPlayer()
    {
        Task.Run(() => new AudioPlayerService())
            .ContinueWith(t => Dispatcher.UIThread.Post(() => OnReady(t)), TaskScheduler.Default);
    }

    private void OnReady(Task<AudioPlayerService> task)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            _error = "Audio engine not found. On Linux install VLC (libvlc); on Windows reinstall the app. "
                     + task.Exception?.GetBaseException().Message;
            var hadPending = _pendingSource != null;
            _pendingSource = null;
            if (hadPending) Failed?.Invoke();
            return;
        }

        var player = task.Result;
        if (_disposed)
        {
            player.Dispose();
            return;
        }

        player.Ended += () => Ended?.Invoke();
        player.Failed += () => Failed?.Invoke();
        player.SetVolume(_volume);
        player.ApplyEqualizer(_eqEnabled, _preamp, _gains);
        _inner = player;
        StartupTimer.Mark("Audio engine ready");

        if (_pendingSource is { } source)
        {
            _pendingSource = null;
            try { player.Play(source); }
            catch { Failed?.Invoke(); }
        }
    }

    public long TimeMs => _inner?.TimeMs ?? 0;
    public long LengthMs => _inner?.LengthMs ?? 0;

    public void Play(string source)
    {
        if (_error != null) throw new InvalidOperationException(_error);
        if (_inner is null) { _pendingSource = source; return; }   // engine still starting: play as soon as it is up
        _inner.Play(source);
    }

    public void Stop()
    {
        _pendingSource = null;
        _inner?.Stop();
    }

    public void SetPaused(bool paused) => _inner?.SetPaused(paused);
    public void SeekFraction(double fraction) => _inner?.SeekFraction(fraction);

    public void SetVolume(int volume)
    {
        _volume = Math.Clamp(volume, 0, 100);
        _inner?.SetVolume(_volume);
    }

    public void ApplyEqualizer(bool enabled, double preampDb, IReadOnlyList<double> gainsDb)
    {
        _eqEnabled = enabled;
        _preamp = preampDb;
        _gains = gainsDb.ToArray();
        _inner?.ApplyEqualizer(enabled, preampDb, gainsDb);
    }

    public void Dispose()
    {
        _disposed = true;
        _inner?.Dispose();
    }
}
