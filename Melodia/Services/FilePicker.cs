using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Melodia.Services;

public static class FilePicker
{
    public static Task<List<string>> PickAudioAsync(string title, bool multiple) =>
        PickAsync(title,
            new[] { "*.mp3", "*.flac", "*.wav", "*.m4a", "*.ogg", "*.opus", "*.aac", "*.wma",
                    "*.MP3", "*.FLAC", "*.WAV", "*.M4A", "*.OGG" },
            "Audio files", multiple);

    public static async Task<List<string>> PickAsync(string title, string[] patterns, string typeName, bool multiple)
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window is null) return new List<string>();

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = multiple,
            FileTypeFilter = new[] { new FilePickerFileType(typeName) { Patterns = patterns } }
        });

        return files.Select(f => f.TryGetLocalPath())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => p!)
                    .ToList();
    }
}
