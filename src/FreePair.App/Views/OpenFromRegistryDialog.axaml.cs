using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

public partial class OpenFromRegistryDialog : Window
{
    public OpenFromRegistryDialog()
    {
        InitializeComponent();
    }

    public OpenFromRegistryDialog(OpenFromRegistryViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OpenFromRegistryViewModel vm)
        {
            Close((OpenFromRegistryViewModel?)null);
            return;
        }
        if (!vm.TryValidate()) return;
        Close(vm);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((OpenFromRegistryViewModel?)null);
}
