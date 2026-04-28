using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

public partial class UscfExportDialog : Window
{
    public UscfExportDialog()
    {
        InitializeComponent();
    }

    public UscfExportDialog(UscfExportViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UscfExportViewModel vm)
        {
            Close((UscfExportViewModel?)null);
            return;
        }
        if (!vm.TryValidate()) return;
        Close(vm);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((UscfExportViewModel?)null);
}
