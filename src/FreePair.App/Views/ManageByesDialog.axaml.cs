using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Modal dialog for editing a single player's requested byes across
/// all future unpaired rounds. Bound to a <see cref="ManageByesViewModel"/>;
/// returns the VM itself on Save (so the caller can read
/// <see cref="ManageByesViewModel.BuildDiffs"/>) or <c>null</c> on Cancel.
/// </summary>
public partial class ManageByesDialog : Window
{
    public ManageByesDialog()
    {
        InitializeComponent();
    }

    public ManageByesDialog(ManageByesViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnSave(object? sender, RoutedEventArgs e) =>
        Close(DataContext as ManageByesViewModel);

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((ManageByesViewModel?)null);
}
