using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FreePair.App.Views;

public partial class SectionView : UserControl
{
    public SectionView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handler for the small "⧉" copy buttons embedded in Players-tab
    /// cells (USCF, Name, Rating, Team, Email, Phone). Reads the value
    /// to copy from the sender's <see cref="Control.Tag"/>, coerces it
    /// to a string (so both string bindings and the numeric Rating
    /// work), and writes it to the application clipboard via the
    /// hosting <see cref="TopLevel"/>. Uses Avalonia 12's
    /// <see cref="DataTransfer"/> + <see cref="IClipboard.SetDataAsync"/>
    /// API.
    /// </summary>
    private async void OnCopyFieldClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var text = btn.Tag switch
        {
            string s => s,
            null => null,
            var other => other.ToString(),
        };
        if (string.IsNullOrEmpty(text)) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;
        try
        {
            var transfer = new DataTransfer();
            transfer.Add(DataTransferItem.CreateText(text));
            await clipboard.SetDataAsync(transfer);
        }
        catch { /* best effort — clipboard access can fail on some hosts */ }
    }

    /// <summary>
    /// Clears the Players-tab filter text box via its bound VM property.
    /// </summary>
    private void OnClearPlayerFilter(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SectionViewModel vm)
        {
            vm.PlayerFilter = string.Empty;
        }
    }
}
