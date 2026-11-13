using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>The Equalizer page: 10 bands, preamp, presets. Applied live and saved (debounced).</summary>
public partial class EqualizerViewModel : ViewModelBase
{
    private readonly IAudioPlayer? _audio;
    private readonly Action<EqualizerSettings> _persist;
    private readonly DispatcherTimer _saveTimer;
    private bool _lock;

    public ObservableCollection<EqBandViewModel> Bands { get; } = new();
    public ObservableCollection<ChipViewModel> PresetChips { get; } = new();

    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private double _preamp;
    [ObservableProperty] private string _preset = "Flat";

    public string ToggleText => Enabled ? "Equalizer: On" : "Equalizer: Off";
    public string PreampText => $"{Preamp:+0.#;-0.#;0} dB";
    public string PresetText => $"Preset: {Preset}";

    public EqualizerViewModel(IAudioPlayer? audio, EqualizerSettings settings, Action<EqualizerSettings> persist)
    {
        _audio = audio;
        _persist = persist;
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); _persist(Snapshot()); };

        _lock = true;
        for (var i = 0; i < EqualizerPresets.BandLabels.Length; i++)
            Bands.Add(new EqBandViewModel(EqualizerPresets.BandLabels[i], i < settings.Gains.Count ? settings.Gains[i] : 0, OnBandChanged));
        Enabled = settings.Enabled;
        Preamp = settings.Preamp;
        Preset = string.IsNullOrWhiteSpace(settings.Preset) ? "Flat" : settings.Preset;
        foreach (var preset in EqualizerPresets.All)
            PresetChips.Add(new ChipViewModel(preset.Name, preset.Name == Preset, name => { SelectPreset(name); return Task.CompletedTask; }));
        _lock = false;
        Apply();
    }

    partial void OnEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(ToggleText));
        Changed();
    }

    partial void OnPreampChanged(double value)
    {
        OnPropertyChanged(nameof(PreampText));
        Changed();
    }

    partial void OnPresetChanged(string value) => OnPropertyChanged(nameof(PresetText));

    private void OnBandChanged()
    {
        if (_lock) return;
        SetPresetName("Custom");
        Changed();
    }

    private void Changed()
    {
        if (_lock) return;
        Apply();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void Apply() => _audio?.ApplyEqualizer(Enabled, Preamp, Bands.Select(b => b.Gain).ToArray());

    private void SetPresetName(string name)
    {
        Preset = name;
        foreach (var c in PresetChips) c.IsSelected = c.Name == name;
    }

    public void SelectPreset(string name)
    {
        var preset = EqualizerPresets.All.FirstOrDefault(p => p.Name == name);
        if (preset.Gains is null) return;
        _lock = true;
        for (var i = 0; i < Bands.Count && i < preset.Gains.Length; i++) Bands[i].Gain = preset.Gains[i];
        _lock = false;
        SetPresetName(name);
        Changed();
    }

    [RelayCommand] private void Toggle() => Enabled = !Enabled;

    [RelayCommand]
    private void Reset()
    {
        _lock = true;
        Preamp = 0;
        _lock = false;
        SelectPreset("Flat");
    }

    private EqualizerSettings Snapshot() => new()
    {
        Enabled = Enabled,
        Preset = Preset,
        Preamp = Preamp,
        Gains = Bands.Select(b => b.Gain).ToList()
    };

    /// <summary>Saves right now (used on shutdown so the last tweak isn't lost).</summary>
    public void Flush()
    {
        _saveTimer.Stop();
        _persist(Snapshot());
    }
}
