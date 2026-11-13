using System.Diagnostics;

namespace Melodia.Services;

/// <summary>
/// Optional startup timing. Set the environment variable MELODIA_STARTUP_LOG=1, start the app, then read
/// %AppData%\Melodia\startup.log: each line shows the milliseconds since the process started.
/// Does nothing (and costs nothing) when the variable is not set.
/// </summary>
public static class StartupTimer
{
    private static readonly string? LogPath =
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MELODIA_STARTUP_LOG"))
            ? null
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Melodia", "startup.log");

    public static void Mark(string what)
    {
        if (LogPath is null) return;
        try
        {
            var ms = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff}  {ms,7:0} ms  {what}{Environment.NewLine}");
        }
        catch { /* timing is best effort */ }
    }
}
