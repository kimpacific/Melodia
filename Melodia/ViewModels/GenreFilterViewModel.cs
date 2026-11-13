using System.Collections.ObjectModel;
using Melodia.Services;

namespace Melodia.ViewModels;

/// <summary>The row of genre cards shown on Home and on every list page.</summary>
public sealed class GenreFilterViewModel : ViewModelBase
{
    private readonly Func<string, Task> _onSelected;

    public ObservableCollection<GenreCardViewModel> Cards { get; } = new();
    public string Selected { get; private set; } = GenreFilterService.All;

    /// <summary>"All" alone isn't worth showing.</summary>
    public bool HasCards => Cards.Count > 1;

    public GenreFilterViewModel(Func<string, Task> onSelected) => _onSelected = onSelected;

    public void SetGenres(IEnumerable<string> names, string selected)
    {
        Selected = selected;
        Cards.Clear();
        Cards.Add(new GenreCardViewModel(GenreFilterService.All,
            string.Equals(selected, GenreFilterService.All, StringComparison.OrdinalIgnoreCase), SelectAsync));
        foreach (var n in names)
            Cards.Add(new GenreCardViewModel(n, string.Equals(n, selected, StringComparison.OrdinalIgnoreCase), SelectAsync));
        OnPropertyChanged(nameof(HasCards));
    }

    private async Task SelectAsync(string genre)
    {
        Selected = genre;
        foreach (var c in Cards) c.IsSelected = string.Equals(c.Name, genre, StringComparison.OrdinalIgnoreCase);
        await _onSelected(genre);
    }
}
