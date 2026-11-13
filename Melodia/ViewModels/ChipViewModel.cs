using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Melodia.ViewModels;

/// <summary>A selectable pill (used for equalizer presets).</summary>
public partial class ChipViewModel : ViewModelBase
{
    private readonly Func<string, Task> _onSelect;
    public string Name { get; }

    [ObservableProperty] private bool _isSelected;

    public ChipViewModel(string name, bool isSelected, Func<string, Task> onSelect)
    {
        Name = name;
        _isSelected = isSelected;
        _onSelect = onSelect;
    }

    [RelayCommand] private Task Select() => _onSelect(Name);
}
