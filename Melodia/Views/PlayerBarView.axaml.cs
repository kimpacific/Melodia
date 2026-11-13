using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Melodia.ViewModels;

namespace Melodia.Views;

public partial class PlayerBarView : UserControl
{
    public PlayerBarView()
    {
        InitializeComponent();

        // While the user drags the progress slider, don't let the playback timer fight with it.
        var slider = this.FindControl<Slider>("ProgressSlider");
        if (slider is null) return;
        slider.AddHandler(PointerPressedEvent, (_, _) => SetScrubbing(true), RoutingStrategies.Tunnel);
        slider.AddHandler(PointerReleasedEvent, (_, _) => SetScrubbing(false), RoutingStrategies.Tunnel);
        slider.AddHandler(PointerCaptureLostEvent, (_, _) => SetScrubbing(false), RoutingStrategies.Tunnel);
    }

    private void SetScrubbing(bool value)
    {
        if (DataContext is MainWindowViewModel vm) vm.Player.IsScrubbing = value;
    }
}
