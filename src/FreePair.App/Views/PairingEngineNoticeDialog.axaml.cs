using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FreePair.App.Views;

/// <summary>
/// First-run / on-demand notice that explains FreePair drives
/// pairings through the BBP Pairings engine (FIDE Dutch System).
/// Returns the final state of "Don't show this again" via
/// <see cref="Window.Close(object?)"/> so the caller can update
/// <c>AppSettings.HasAcknowledgedPairingEngineNotice</c>.
/// </summary>
public partial class PairingEngineNoticeDialog : Window
{
    public PairingEngineNoticeDialog()
    {
        InitializeComponent();
    }

    private void OnAcknowledge(object? sender, RoutedEventArgs e)
    {
        // True means "Don't show this again" was checked; we
        // persist that as HasAcknowledgedPairingEngineNotice = true.
        Close(DontShowAgainBox.IsChecked == true);
    }
}
