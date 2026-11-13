# Melodia

A desktop music player built with C# / .NET 10 and Avalonia UI (MVVM), styled after the reference design.

## Features
- **Full-length songs** streamed from the free Audius API (no key needed)
- Home: recommended artists, top chart and a trending-albums panel (click an album's tracks to play)
- **Genre cards**: a scrollable row of cards (background picture + icon + name) on Home, Explore, search results and every list. Home, Explore and search filter on Audius's side; Songs, Favorite, playlists, album and artist pages filter by each song's genre tag. The pictures are `Assets/Genres/<name>.jpg`: replace any of them with your own photo (same file name, about 320x180) and rebuild
- **Artist tab**: type an artist's name, open them and play their songs (a single exact match opens straight away). Without a query it shows this week's popular artists
- **Albums tab**: grid of trending albums; click one to see and play its songs
- **Shuffle and repeat** (off / all / one) in the player bar
- Playlists can be **renamed**, and songs removed with the x on each row
- **10-band equalizer** (sidebar > Equalizer): live sliders, preamp, 12 presets, on/off switch, saved between sessions
- Search, Explore (trending this week), favorites, Songs library
- Playlists you create yourself, with local audio files added through a file picker (mp3, flac, wav, m4a, ogg, opus, aac, wma); **Import Playlist** reads `.m3u` / `.m3u8`
- Back / forward navigation, seekable progress bar, volume, next / previous, auto-advance
- Playlists, favorites and equalizer settings are saved in `library.json` (Windows: `%AppData%\Melodia`, Linux: `~/.config/Melodia`, macOS: `~/Library/Application Support/Melodia`). Saves are atomic and a `.bak` copy is kept; a damaged file is preserved as `.corrupt` instead of being overwritten
- Local songs that were moved or deleted are dimmed and skipped during playback

## Requirements
- .NET 10 SDK or newer (`dotnet --version`)
- Windows 10+: nothing else. Audio uses LibVLC, shipped by the NuGet package
- Linux only: `sudo apt install vlc libvlc-dev`
- macOS: install VLC; the LibVLC package for Mac is referenced but untested

## Run
```powershell
dotnet run
```

## Notes
- Audius hosts independent artists, so the catalog is not the big mainstream catalog. Artist names are always shown, as Audius asks.
- Optional: for higher Audius rate limits, create a free key at https://api.audius.co/plans and set the environment variable `AUDIUS_BEARER_TOKEN`.
- The app icon is `Assets/logo.ico` (and `logo.png`).

## Project layout
- `Services/`: Audius client, LibVLC player, library store, genre catalog, `PlaybackQueue` (shuffle/repeat), `NavigationHistory`. The three that touch the outside world (`IMusicCatalog`, `ILibraryStore`, `IAudioPlayer`) are interfaces, wired together in `App.axaml.cs`
- `ViewModels/`: `MainWindowViewModel` (shell + navigation) owns `PlayerViewModel`, `LibraryViewModel`, `EqualizerViewModel`, `AlbumPanelViewModel` and the page view models (`HomeViewModel`, `TrackListViewModel`, `ArtistsViewModel`, `AlbumsViewModel`)
- `Views/`: one user control per page / region; the shell picks the page from the page view model's type

## Tests
```powershell
dotnet test ../Melodia.Tests
```
Covers the queue (shuffle/repeat), navigation history, genre matching, the library store and the M3U reader.

## Troubleshooting
- "Couldn't reach Audius": press **Try again** on Home. The app retries and falls back to other Audius hosts automatically, so a persistent error usually means no internet access or a firewall/proxy
