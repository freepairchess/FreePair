using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Modal review dialog the TD opens via the 🔢 Renumber boards
/// toolbar button. Surfaces each section's current starting board,
/// player count, max boards needed, and a non-blocking conflict
/// warning when ranges overlap. Cancel returns null; Apply returns
/// the populated VM so the caller can read the chosen values.
/// </summary>
public partial class RenumberBoardsDialog : Window
{
    public RenumberBoardsDialog()
    {
        InitializeComponent();
    }

    public RenumberBoardsDialog(RenumberBoardsViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnApply(object? sender, RoutedEventArgs e) =>
        Close(DataContext as RenumberBoardsViewModel);

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((RenumberBoardsViewModel?)null);
}
