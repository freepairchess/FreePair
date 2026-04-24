using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Modal dialog for editing (or, in a later commit, adding) player
/// identity / contact fields. Bound to a
/// <see cref="PlayerFormViewModel"/>; Save returns the VM so the
/// caller reads the validated fields, Cancel returns <c>null</c>.
/// </summary>
public partial class PlayerFormDialog : Window
{
    public PlayerFormDialog()
    {
        InitializeComponent();
    }

    public PlayerFormDialog(PlayerFormViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PlayerFormViewModel vm) { Close((PlayerFormViewModel?)null); return; }
        if (!vm.TryValidate(out _, out _)) return; // error message bound to the TextBlock
        Close(vm);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((PlayerFormViewModel?)null);
}
