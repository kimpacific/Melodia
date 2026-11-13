using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Melodia.Services;
using Melodia.ViewModels;
using Melodia.Views;

namespace Melodia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Composition root. Startup is kept light on purpose: the window is created straight away, the audio
    /// engine starts on a background thread, and the first page loads after the first frame is drawn.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel(new AudiusService(), new PlaylistStore(), new LazyAudioPlayer(), "");
            desktop.MainWindow = new MainWindow { DataContext = vm };
            desktop.Exit += (_, _) => vm.Dispose();

            // Background priority runs after the window has been rendered once.
            Dispatcher.UIThread.Post(vm.Start, DispatcherPriority.Background);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
