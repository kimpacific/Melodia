using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodia.Models;

namespace Melodia.ViewModels;

public partial class PlaylistViewModel : ViewModelBase
{
    private readonly Action<PlaylistViewModel> _open;
    private readonly Action<PlaylistViewModel> _addCurrent;

    public PlaylistData Data { get; }
    public string Name => Data.Name;

    [ObservableProperty] private bool _isActive;

    public PlaylistViewModel(PlaylistData data, Action<PlaylistViewModel> open, Action<PlaylistViewModel> addCurrent)
    {
        Data = data;
        _open = open;
        _addCurrent = addCurrent;
    }

    public void Rename(string name)
    {
        Data.Name = name;
        OnPropertyChanged(nameof(Name));
    }

    [RelayCommand] private void Open() => _open(this);
    [RelayCommand] private void AddCurrent() => _addCurrent(this);
}
