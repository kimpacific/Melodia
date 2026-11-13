using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>The player bar: queue, now-playing info, progress, volume, shuffle and repeat.</summary>
public partial class PlayerViewModel : ViewModelBase, IDisposable
{
    private readonly IAudioPlayer? _audio;
    private readonly string _audioError;
    private readonly Func<string, bool> _isFavorite;
    private readonly Action<TrackViewModel> _toggleFavorite;
    private readonly Func<TrackViewModel?> _defaultTrack;
    private readonly PlaybackQueue<TrackViewModel> _queue = new();
    private readonly DispatcherTimer _timer;

    private TrackViewModel? _current;
    private bool _ended;
    private bool _suppressSeek;
    private int _failStreak;
    private string? _skipNote;

    /// <summary>True while the user is dragging the progress slider.</summary>
    public bool IsScrubbing { get; set; }

    /// <summary>The song that is loaded (playing or paused).</summary>
    public TrackViewModel? Current => _current;

    /// <summary>Raised when a different song starts, so visible rows can update their "now playing" mark.</summary>
    public event Action? CurrentChanged;

    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private string _nowTitle = "Nothing playing";
    [ObservableProperty] private Bitmap? _nowCover;
    [ObservableProperty] private bool _isNowFavorite;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private double _volume = 70;
    [ObservableProperty] private string _elapsedText = "0:00";
    [ObservableProperty] private string _totalText = "0:00";
    [ObservableProperty] private bool _isShuffle;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(NowSubtitle))]
    private string _nowArtist = "Pick a song to start";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStatus), nameof(NowSubtitle))]
    private string _statusMessage = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsRepeatActive), nameof(IsRepeatOne), nameof(RepeatTooltip))]
    private RepeatMode _repeat = RepeatMode.Off;

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);
    /// <summary>Shows a playback problem in place of the artist name until the next song starts.</summary>
    public string NowSubtitle => HasStatus ? StatusMessage : NowArtist;
    public bool IsRepeatActive => Repeat != RepeatMode.Off;
    public bool IsRepeatOne => Repeat == RepeatMode.One;
    public string RepeatTooltip => Repeat switch
    {
        RepeatMode.All => "Repeat: all songs",
        RepeatMode.One => "Repeat: this song",
        _ => "Repeat: off"
    };

    public PlayerViewModel(IAudioPlayer? audio, string audioError, Func<string, bool> isFavorite,
                           Action<TrackViewModel> toggleFavorite, Func<TrackViewModel?> defaultTrack)
    {
        _audio = audio;
        _audioError = audioError;
        _isFavorite = isFavorite;
        _toggleFavorite = toggleFavorite;
        _defaultTrack = defaultTrack;

        if (_audio != null)
        {
            _audio.Ended += OnTrackEnded;
            _audio.Failed += OnTrackFailed;
            _audio.SetVolume((int)Volume);
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => UpdateProgress();
        _timer.Start();
    }

    // ======================= starting playback =======================

    public void PlayTrack(TrackViewModel track)
    {
        _queue.Load(track.Queue ?? new[] { track }, track);
        _failStreak = 0;
        StartCurrent();
    }

    private void StartCurrent()
    {
        var track = _queue.Current;
        if (track is null) return;

        if (_audio is null) { StatusMessage = _audioError; return; }

        var source = track.Model.Source;
        if (string.IsNullOrWhiteSpace(source)) { Fail(track, "it has no playable source"); return; }
        if (track.Model.IsLocal && !File.Exists(source)) { Fail(track, "the file was moved or deleted"); return; }

        try { _audio.Play(source); }
        catch (Exception ex) { Fail(track, ex.Message); return; }

        _current = track;
        _ended = false;
        IsPlaying = true;
        NowTitle = track.Title;
        NowArtist = track.Artist;
        StatusMessage = _skipNote ?? "";
        _skipNote = null;
        IsNowFavorite = _isFavorite(track.Model.Id);
        NowCover = track.Cover;
        SetProgress(0);
        ElapsedText = "0:00";
        TotalText = track.DurationText;
        _ = UpdateNowCoverAsync(track);
        CurrentChanged?.Invoke();
    }

    private async Task UpdateNowCoverAsync(TrackViewModel track)
    {
        var cover = await track.CoverTask;
        if (_current == track) NowCover = cover;
    }

    /// <summary>A song can't be played: skip to the next one, but give up after a few failures in a row.</summary>
    private void Fail(TrackViewModel track, string reason)
    {
        _failStreak++;
        if (_failStreak < 3 && _queue.HasNext)
        {
            _skipNote = $"Skipped “{track.Title}”: {reason}.";
            _queue.MoveNext(auto: false);
            StartCurrent();
            return;
        }

        try { _audio?.Stop(); } catch { /* ignore */ }
        IsPlaying = false;
        StatusMessage = $"Couldn't play “{track.Title}”: {reason}.";
    }

    // ======================= controls =======================

    [RelayCommand]
    private void TogglePlay()
    {
        if (_current is null)
        {
            var first = _defaultTrack();
            if (first != null) PlayTrack(first);
            return;
        }
        if (_audio is null) return;

        if (_ended) { StartCurrent(); return; }
        if (IsPlaying) { _audio.SetPaused(true); IsPlaying = false; }
        else { _audio.SetPaused(false); IsPlaying = true; }
    }

    [RelayCommand]
    private void Next()
    {
        if (_queue.Count == 0) return;
        _failStreak = 0;
        _queue.MoveNext(auto: false);
        StartCurrent();
    }

    [RelayCommand]
    private void Previous()
    {
        if (_queue.Count == 0) return;
        if (_audio != null && _audio.TimeMs > 3000) { _audio.SeekFraction(0); return; }
        _failStreak = 0;
        _queue.MovePrevious();
        StartCurrent();
    }

    [RelayCommand]
    private void ToggleShuffle()
    {
        IsShuffle = !IsShuffle;
        _queue.SetShuffle(IsShuffle);
    }

    [RelayCommand]
    private void CycleRepeat()
    {
        Repeat = Repeat switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.Off
        };
        _queue.Repeat = Repeat;
    }

    [RelayCommand]
    private void ToggleNowFavorite()
    {
        if (_current != null) _toggleFavorite(_current);
    }

    /// <summary>Called by the shell after a favorite changes, so the heart in the player bar stays in sync.</summary>
    public void RefreshFavorite() =>
        IsNowFavorite = _current != null && _isFavorite(_current.Model.Id);

    // ======================= engine events =======================

    private void OnTrackEnded()
    {
        var next = _queue.MoveNext(auto: true);
        if (next != null)
        {
            StartCurrent();
            return;
        }
        _ended = true;
        IsPlaying = false;
        SetProgress(0);
        ElapsedText = "0:00";
    }

    private void OnTrackFailed()
    {
        var track = _queue.Current;
        if (track != null) Fail(track, "it may be restricted or your connection dropped");
    }

    // ======================= progress =======================

    private void SetProgress(double value)
    {
        _suppressSeek = true;
        Progress = value;
        _suppressSeek = false;
    }

    private void UpdateProgress()
    {
        if (_audio is null || _current is null || !IsPlaying) return;
        var length = _audio.LengthMs;
        var time = _audio.TimeMs;
        if (time > 0) _failStreak = 0;
        if (length > 0)
        {
            if (!IsScrubbing) SetProgress(Math.Clamp(time * 100.0 / length, 0, 100));
            TotalText = Format(length);
        }
        ElapsedText = Format(time);
    }

    partial void OnProgressChanged(double value)
    {
        if (_suppressSeek || _audio is null || _current is null) return;
        _audio.SeekFraction(value / 100.0);
    }

    partial void OnVolumeChanged(double value) => _audio?.SetVolume((int)value);

    private static string Format(long ms)
    {
        var ts = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}";
    }

    public void Dispose() => _timer.Stop();
}
