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

    /// <summary>
    /// Hyperlink click on the prefilled event name — shells out to
    /// the registry's public details page via the OS default
    /// browser. Best-effort; silently ignores when there's no URL
    /// or the launch fails. Same idiom as the browse-events grid.
    /// </summary>
    private void OnEventNameClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OpenFromRegistryViewModel vm) return;
        var url = vm.PrefilledEventWebUrl;
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch
        {
            // No default browser registered / sandboxed shell — fine.
        }
    }
}
