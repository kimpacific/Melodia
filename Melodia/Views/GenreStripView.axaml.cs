using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Melodia.Views;

public partial class GenreStripView : UserControl
{
    public GenreStripView()
    {
        InitializeComponent();
        // Let the mouse wheel scroll the strip sideways while the pointer is over it.
        this.FindControl<ScrollViewer>("Scroller")?.AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
    }

    private static void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        var maxX = sv.Extent.Width - sv.Viewport.Width;
        if (maxX <= 0 || Math.Abs(e.Delta.Y) <= Math.Abs(e.Delta.X)) return;

        var x = Math.Clamp(sv.Offset.X - e.Delta.Y * 90, 0, maxX);
        sv.Offset = new Vector(x, sv.Offset.Y);
        e.Handled = true;
    }
}
