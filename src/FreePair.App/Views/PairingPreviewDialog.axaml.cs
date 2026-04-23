using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;
using FreePair.Core.Tournaments;

namespace FreePair.App.Views;

/// <summary>
/// Pre-commit pairing preview dialog (SwissSysFeatureParity UI for
/// #4 colour override, #6 manual board re-pair, #8 late ½-pt bye).
/// Returns the TD-modified <see cref="Tournament"/> on Commit, or
/// <c>null</c> when the TD cancels (the host then reverts via
/// <see cref="TournamentMutations.DeleteLastRound"/>).
/// </summary>
public partial class PairingPreviewDialog : Window
{
    public PairingPreviewDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Wires the dialog to a <see cref="PairingPreviewViewModel"/>
    /// and updates the header label. Call once after construction.
    /// </summary>
    public void Configure(PairingPreviewViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        DataContext = vm;
        if (HeaderText is not null)
        {
            HeaderText.Text = $"Section '{vm.SectionName}' — round {vm.Round} preview";
        }
    }

    private PairingPreviewViewModel? Vm => DataContext as PairingPreviewViewModel;

    private void OnRowSwapColors(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || sender is not Button btn || btn.Tag is not PairingPreviewRow row)
            return;
        try { Vm.SwapColors(row); ClearError(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OnRowHalfByeWhite(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || sender is not Button btn || btn.Tag is not PairingPreviewRow row)
            return;
        try { Vm.ConvertHalfBye(row, row.WhitePair); ClearError(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OnRowHalfByeBlack(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || sender is not Button btn || btn.Tag is not PairingPreviewRow row)
            return;
        try { Vm.ConvertHalfBye(row, row.BlackPair); ClearError(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OnSwapBoards(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var a = (int)(SwapBoardA.Value ?? 0);
        var b = (int)(SwapBoardB.Value ?? 0);
        if (a == 0 || b == 0) return;
        try { Vm.SwapBoards(a, b); ClearError(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OnCommit(object? sender, RoutedEventArgs e) =>
        Close(Vm?.Tournament);

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((Tournament?)null);

    private void ShowError(string msg)
    {
        if (SwapErrorText is not null) SwapErrorText.Text = msg;
    }

    private void ClearError()
    {
        if (SwapErrorText is not null) SwapErrorText.Text = string.Empty;
    }
}
