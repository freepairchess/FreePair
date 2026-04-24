using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Modal dialog for starting a brand-new tournament. After the TD
/// fills in a title (and optionally a first section), the caller
/// shows a native save-file picker, then builds the Tournament
/// domain object and asks <see cref="SwissSysTournamentWriter"/> to
/// write it to the chosen path.
/// </summary>
public partial class NewEventDialog : Window
{
    public NewEventDialog()
    {
        InitializeComponent();
    }

    public NewEventDialog(NewEventViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewEventViewModel vm) { Close((NewEventViewModel?)null); return; }
        if (!vm.TryValidate(out _)) return;
        Close(vm);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((NewEventViewModel?)null);
}
