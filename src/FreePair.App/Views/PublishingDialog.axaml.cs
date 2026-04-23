using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

/// <summary>
/// Window that hosts the Publish-online form. The VM lives in
/// <see cref="ViewModels.PublishingDialogViewModel"/> and is wired by
/// the caller (typically <c>TournamentViewModel</c>) before
/// <c>ShowDialog</c> is invoked. Returns the VM on close so the
/// caller can inspect the chosen auto-flags.
/// </summary>
public partial class PublishingDialog : Window
{
    public PublishingDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is PublishingDialogViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            UpdateStatusBannerClasses(vm);
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is PublishingDialogViewModel vm &&
            (e.PropertyName == nameof(PublishingDialogViewModel.StatusIsError)
             || e.PropertyName == nameof(PublishingDialogViewModel.StatusMessage)))
        {
            UpdateStatusBannerClasses(vm);
        }
    }

    /// <summary>
    /// Flips the <c>error</c> CSS-like class on the status banner so
    /// the style selector in the XAML re-tints it red when the last
    /// publish attempt failed.
    /// </summary>
    private void UpdateStatusBannerClasses(PublishingDialogViewModel vm)
    {
        if (StatusBanner is null) return;
        if (vm.StatusIsError) StatusBanner.Classes.Add("error");
        else                  StatusBanner.Classes.Remove("error");
    }

    private void OnClose(object? sender, RoutedEventArgs e)
        => Close(DataContext);

    /// <summary>
    /// Opens the post-upload verification URL (bound to the button's
    /// Tag via <c>PublishingDialogViewModel.PublishedUrl</c>) in the
    /// user's default browser via the OS shell.
    /// </summary>
    private void OnOpenPublishedUrl(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string url || string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { /* best-effort */ }
    }
}

