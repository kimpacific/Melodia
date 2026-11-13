using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Melodia.ViewModels;

namespace Melodia.Views;

public partial class TrackListView : UserControl
{
    public TrackListView() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is TrackListViewModel vm) vm.PropertyChanged += OnChanged;
    }

    // focus the rename box when "Rename" is clicked
    private void OnChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TrackListViewModel.IsRenaming) || sender is not TrackListViewModel { IsRenaming: true }) return;
        Dispatcher.UIThread.Post(() =>
        {
            var box = this.FindControl<TextBox>("RenameBox");
            box?.Focus();
            box?.SelectAll();
        });
    }
}
