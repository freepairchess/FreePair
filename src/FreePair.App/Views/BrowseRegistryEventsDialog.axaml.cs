using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Modal dialog that lists online events from the selected
/// registry. Loads the list on open + whenever the TD changes the
/// registry or clicks Refresh. OK returns the VM so the caller can
/// kick off a download using the selected event + passcode; Cancel
/// returns null.
/// </summary>
public partial class BrowseRegistryEventsDialog : Window
{
    public BrowseRegistryEventsDialog()
    {
        InitializeComponent();
    }

    public BrowseRegistryEventsDialog(BrowseRegistryEventsViewModel vm) : this()
    {
        DataContext = vm;
        Opened += async (_, _) => await LoadAsync();
        vm.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(BrowseRegistryEventsViewModel.SelectedRegistry))
            {
                await LoadAsync();
            }
        };
    }

    private async Task LoadAsync()
    {
        if (DataContext is not BrowseRegistryEventsViewModel vm) return;
        if (vm.SelectedRegistry is null) return;

        vm.IsLoading = true;
        vm.ErrorMessage = null;
        try
        {
            var events = await vm.SelectedRegistry.ListEventsAsync();
            vm.SetEvents(events);
        }
        catch (Exception ex)
        {
            vm.SetEvents(System.Array.Empty<FreePair.Core.Registries.RegistryEvent>());
            vm.ErrorMessage = ex.Message;
        }
        finally
        {
            vm.IsLoading = false;
        }
    }

    private async void OnRefresh(object? sender, RoutedEventArgs e) =>
        await LoadAsync();

    /// <summary>
    /// Per-row Open click. Records the chosen event on the VM and
    /// closes the dialog so the caller can pop the by-id Open
    /// dialog with the event ID pre-filled.
    /// </summary>
    private void OnRowOpenClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not BrowseRegistryEventsViewModel vm)
        {
            Close((BrowseRegistryEventsViewModel?)null);
            return;
        }
        if (sender is Avalonia.Controls.Button b && b.Tag is FreePair.Core.Registries.RegistryEvent ev)
        {
            vm.ChosenEvent = ev;
            Close(vm);
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((BrowseRegistryEventsViewModel?)null);
}
