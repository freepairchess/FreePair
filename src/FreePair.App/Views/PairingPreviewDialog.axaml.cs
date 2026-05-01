using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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
    /// <summary>Drag payload: a string of the form
    /// <c>"{colour}:{board}"</c> where colour is "W" or "B".
    /// Avalonia 12 application-scoped DataFormats reject slashes
    /// in the identifier (validated at construction time —
    /// otherwise the static initializer throws and the whole
    /// dialog fails to load), so we use a dotted reverse-DNS
    /// shape: app name + payload kind.</summary>
    private static readonly DataFormat<string> PlayerChipFormat =
        DataFormat.CreateStringApplicationFormat("freepair.player-chip");

    public PairingPreviewDialog()
    {
        InitializeComponent();
        // Drop targets are declared on the chips themselves
        // (DragDrop.AllowDrop="True"). We hook the events at the
        // window level so a single handler can see the source
        // payload and walk to the target chip.
        AddHandler(DragDrop.DragOverEvent, OnPlayerChipDragOver);
        AddHandler(DragDrop.DropEvent, OnPlayerChipDrop);
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

    // ============ drag-and-drop ============

    /// <summary>
    /// Starts a drag from a player chip. The source chip's
    /// <c>Tag</c> ("W" / "B") + DataContext (the
    /// <see cref="PairingPreviewRow"/>) together identify the
    /// (board, colour) pair the TD wants to move; we encode that
    /// as <c>"{colour}:{board}"</c> in the data transfer.
    /// </summary>
    private async void OnPlayerChipPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border chip) return;
        if (!e.GetCurrentPoint(chip).Properties.IsLeftButtonPressed) return;
        if (chip.Tag is not string colour) return;
        if (chip.DataContext is not PairingPreviewRow row) return;

        ClearError();

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(PlayerChipFormat, $"{colour}:{row.Board}"));

        try
        {
            await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Move).ConfigureAwait(true);
        }
        catch
        {
            // DoDragDropAsync can throw if the platform refuses
            // the drag (e.g. the user clicked but didn't actually
            // start moving). Swallow — there's nothing for the TD
            // to act on.
        }
    }

    /// <summary>
    /// Allows the drop only when source and target chips share the
    /// same colour ("W" onto "W" or "B" onto "B"). Cross-colour
    /// drops are silently rejected so the cursor visibly refuses.
    /// </summary>
    private void OnPlayerChipDragOver(object? sender, DragEventArgs e)
    {
        var allowed = TryGetSourceTarget(e, out var srcColour, out _, out var tgtColour, out _)
                      && srcColour == tgtColour;
        e.DragEffects = allowed ? DragDropEffects.Move : DragDropEffects.None;
    }

    /// <summary>
    /// Performs the swap on drop. Same-colour pre-checked by
    /// <see cref="OnPlayerChipDragOver"/>. On rematch error,
    /// prompts the TD to confirm a forced swap; if confirmed, calls
    /// <see cref="PairingPreviewViewModel.SwapBoardsForced"/> which
    /// annotates both pairings with a session note.
    /// </summary>
    private async void OnPlayerChipDrop(object? sender, DragEventArgs e)
    {
        if (Vm is null) return;
        if (!TryGetSourceTarget(e, out var srcColour, out var srcBoard, out var tgtColour, out var tgtBoard))
            return;
        if (srcColour != tgtColour) return;
        if (srcBoard == tgtBoard) return;

        try
        {
            Vm.SwapBoards(srcBoard, tgtBoard);
            ClearError();
            return;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("recreate", StringComparison.OrdinalIgnoreCase))
        {
            // Rematch — ask before forcing.
            var proceed = await PromptForceSwapAsync(srcBoard, tgtBoard).ConfigureAwait(true);
            if (!proceed) { ClearError(); return; }
            try
            {
                Vm.SwapBoardsForced(srcBoard, tgtBoard);
                ClearError();
            }
            catch (Exception inner)
            {
                ShowError(inner.Message);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>
    /// Reads the drag payload and walks up the visual tree from the
    /// drop target to find the nearest player chip
    /// (<see cref="Border"/> with <c>Tag</c>="W"/"B" and a
    /// <see cref="PairingPreviewRow"/> DataContext).
    /// </summary>
    private static bool TryGetSourceTarget(
        DragEventArgs e,
        out string srcColour,
        out int srcBoard,
        out string tgtColour,
        out int tgtBoard)
    {
        srcColour = string.Empty; srcBoard = 0;
        tgtColour = string.Empty; tgtBoard = 0;

        if (e.DataTransfer.TryGetValue(PlayerChipFormat) is not { } raw || string.IsNullOrEmpty(raw))
            return false;
        var parts = raw.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var srcBd)) return false;

        // Walk up the visual tree from e.Source to find the chip
        // Border. Avalonia routes the event through nested visuals
        // so e.Source could be the inner TextBlock or a Run.
        Visual? cursor = e.Source as Visual;
        while (cursor is not null)
        {
            if (cursor is Border br
                && br.Tag is string col
                && (col == "W" || col == "B")
                && br.DataContext is PairingPreviewRow row)
            {
                srcColour = parts[0];
                srcBoard  = srcBd;
                tgtColour = col;
                tgtBoard  = row.Board;
                return true;
            }
            cursor = cursor.GetVisualParent();
        }
        return false;
    }

    private async Task<bool> PromptForceSwapAsync(int boardA, int boardB)
    {
        var dialog = new ConfirmDialog();
        dialog.Configure(
            "Swap recreates a previous game",
            $"Swapping boards {boardA} and {boardB} would recreate a previously-played pairing. " +
            "If both rooms truly need this matchup (e.g. a make-up game), you can proceed and " +
            "FreePair will flag both boards with a warning note until results are entered.\n\n" +
            "Swap anyway?",
            confirmLabel: "Swap with note");
        var result = await dialog.ShowDialog<bool?>(this);
        return result == true;
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
