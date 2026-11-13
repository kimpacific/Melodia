using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Melodia.ViewModels;

namespace Melodia.Views;

public partial class SidebarView : UserControl
{
    public SidebarView() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm) vm.Library.PropertyChanged += OnLibraryChanged;
    }

    // focus the name box as soon as "New Playlist" is clicked
    private void OnLibraryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryViewModel.IsCreatingPlaylist) && sender is LibraryViewModel { IsCreatingPlaylist: true })
            Dispatcher.UIThread.Post(() => this.FindControl<TextBox>("NewPlaylistBox")?.Focus());
    }
}
