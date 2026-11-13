using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace Melodia.Services;

/// <summary>Thin wrapper over LibVLC: plays local files and http(s) streams, with a 10-band equalizer.</summary>
public sealed class AudioPlayerService : IAudioPlayer
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private int _volume = 70;

    private bool _eqEnabled;
    private double _preampDb;
    private double[] _gainsDb = new double[10];

    public event Action? Ended;
    public event Action? Failed;

    public AudioPlayerService()
    {
        Core.Initialize();
        _libVlc = new LibVLC("--no-video");
        _player = new MediaPlayer(_libVlc);
        // VLC raises these on its own threads; hop to the UI thread before touching anything.
        _player.EndReached += (_, _) => Dispatcher.UIThread.Post(() => Ended?.Invoke());
        _player.EncounteredError += (_, _) => Dispatcher.UIThread.Post(() => Failed?.Invoke());
    }

    public long TimeMs => _player.Time;
    public long LengthMs => _player.Length;

    public void Play(string source)
    {
        var isUrl = source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        using var media = isUrl
            ? new Media(_libVlc, new Uri(source))
            : new Media(_libVlc, source, FromType.FromPath);
        _player.Play(media);
        _player.Volume = _volume;
        PushEqualizer();
    }

    public void Stop() => _player.Stop();
    public void SetPaused(bool paused) => _player.SetPause(paused);
    public void SeekFraction(double fraction) => _player.Position = (float)Math.Clamp(fraction, 0, 1);

    public void SetVolume(int volume)
    {
        _volume = Math.Clamp(volume, 0, 100);
        _player.Volume = _volume;
    }

    /// <summary>Sets the equalizer live. Gains are in dB (about -12..+12), one per band.</summary>
    public void ApplyEqualizer(bool enabled, double preampDb, IReadOnlyList<double> gainsDb)
    {
        _eqEnabled = enabled;
        _preampDb = preampDb;
        _gainsDb = gainsDb.ToArray();
        PushEqualizer();
    }

    private void PushEqualizer()
    {
        // A fresh equalizer is flat; when disabled we simply apply the flat one.
        using var eq = new Equalizer();
        if (_eqEnabled)
        {
            eq.SetPreamp((float)_preampDb);
            for (var i = 0; i < _gainsDb.Length && i < 10; i++)
                eq.SetAmp((float)_gainsDb[i], (uint)i);
        }
        _player.SetEqualizer(eq);
    }

    public void Dispose()
    {
        try { _player.Stop(); } catch { }
        _player.Dispose();
        _libVlc.Dispose();
    }
}
