using Avalonia;
using Melodia.Services;

namespace Melodia;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Used by the installer right after copying files: starts LibVLC once so its plugin cache
        // (plugins.dat) is built now, instead of making the user's first launch slow. No window.
        if (args.Contains("--warm-audio"))
        {
            WarmAudio();
            return;
        }

        StartupTimer.Mark("Main entered");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void WarmAudio()
    {
        try
        {
            LibVLCSharp.Shared.Core.Initialize();
            using var vlc = new LibVLCSharp.Shared.LibVLC("--no-video");
        }
        catch
        {
            // nothing to do: the app reports audio problems itself when you press play
        }
    }
}
