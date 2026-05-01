using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Modal review dialog the TD opens via the "Pair all sections"
/// toolbar button. Lists each section with status / progress /
/// start board so the TD can confirm which sections will be paired
/// in a single batch run before committing.
/// </summary>
public partial class PairAllSectionsDialog : Window
{
    public PairAllSectionsDialog()
    {
        InitializeComponent();
    }

    public PairAllSectionsDialog(PairAllSectionsViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnApply(object? sender, RoutedEventArgs e) =>
        Close(DataContext as PairAllSectionsViewModel);

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((PairAllSectionsViewModel?)null);
}
