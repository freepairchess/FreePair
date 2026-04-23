using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FreePair.App.Views;

public partial class EventConfigView : UserControl
{
    public EventConfigView() => InitializeComponent();

    /// <summary>
    /// Click handler for the small "↗ US Chess lookup" button next to
    /// the Organizer ID TextBox. Reads the absolute URL from the
    /// sender's <see cref="Control.Tag"/> (bound to
    /// <c>EventConfigViewModel.UscfAffiliateUrl</c>) and hands it off
    /// to the OS shell so it opens in the user's default browser.
    /// </summary>
    private void OnOpenUrlClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string url || string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            // UseShellExecute:true makes this work cross-platform
            // (Windows: shell open; macOS/Linux: falls back to xdg-open).
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { /* best-effort — silently ignore launch failures */ }
    }
}

