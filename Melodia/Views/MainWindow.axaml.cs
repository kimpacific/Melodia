using Avalonia.Controls;
using Melodia.Services;

namespace Melodia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => StartupTimer.Mark("Window opened");
    }
}
