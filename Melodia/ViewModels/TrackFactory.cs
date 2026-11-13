using Melodia.Models;

namespace Melodia.ViewModels;

/// <summary>Creates track rows wired to the player (play) and the library (favorites).</summary>
public sealed class TrackFactory
{
    private readonly PlayerViewModel _player;
    private readonly LibraryViewModel _library;

    public TrackFactory(PlayerViewModel player, LibraryViewModel library)
    {
        _player = player;
        _library = library;
    }

    public TrackViewModel Create(Track track, int index, IReadOnlyList<TrackViewModel>? queue,
                                 Action<TrackViewModel>? remove = null) =>
        new(track, index, _library.IsFavorite(track.Id), _player.Current?.Model.Id == track.Id,
            _player.PlayTrack, _library.ToggleFavorite)
        {
            Queue = queue,
            RemoveHandler = remove
        };
}
