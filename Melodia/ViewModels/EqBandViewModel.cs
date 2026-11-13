using CommunityToolkit.Mvvm.ComponentModel;

namespace Melodia.ViewModels;

public partial class EqBandViewModel : ViewModelBase
{
    private readonly Action _changed;
    public string Label { get; }

    [ObservableProperty] private double _gain;

    public string GainText => $"{Gain:+0.#;-0.#;0} dB";

    public EqBandViewModel(string label, double gain, Action changed)
    {
        Label = label;
        _gain = gain;
        _changed = changed;
    }

    partial void OnGainChanged(double value)
    {
        OnPropertyChanged(nameof(GainText));
        _changed();
    }
}
