using System.Collections.Concurrent;
using Avalonia.Media.Imaging;

namespace Melodia.Services;

/// <summary>Downloads and caches images. Failures are never cached (so they retry) and the cache is bounded.</summary>
public static class ImageLoader
{
    private const int MaxEntries = 300;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> Cache = new();
    private static readonly ConcurrentQueue<string> Order = new();

    public static async Task<Bitmap?> LoadAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var task = Cache.GetOrAdd(url, u =>
        {
            Order.Enqueue(u);
            return FetchAsync(u);
        });

        var bitmap = await task;
        if (bitmap is null) Cache.TryRemove(url, out _);   // don't remember failures
        Trim();
        return bitmap;
    }

    private static void Trim()
    {
        while (Cache.Count > MaxEntries && Order.TryDequeue(out var oldest))
            Cache.TryRemove(oldest, out _);
    }

    private static async Task<Bitmap?> FetchAsync(string url)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var bytes = await Http.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);
                return new Bitmap(ms);
            }
            catch
            {
                await Task.Delay(400);
            }
        }
        return null;
    }
}
