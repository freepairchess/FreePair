using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Modal dialog for adding a new section (and, later, for editing an
/// existing section's metadata). Bound to a
/// <see cref="SectionFormViewModel"/>; Save returns the VM on
/// success, null on Cancel.
/// </summary>
public partial class SectionFormDialog : Window
{
    public SectionFormDialog()
    {
        InitializeComponent();
    }

    public SectionFormDialog(SectionFormViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SectionFormViewModel vm) { Close((SectionFormViewModel?)null); return; }
        if (!vm.TryValidate(out _)) return;
        Close(vm);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((SectionFormViewModel?)null);
}
