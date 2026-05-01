using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// One row in the pairing-preview dialog: a single board with the white
/// and black assignment, displayed names, and a flag indicating whether
/// the pairing has been mutated by the TD since BBP produced it.
/// </summary>
public sealed record PairingPreviewRow(
    int Board,
    int WhitePair,
    string WhiteName,
    int BlackPair,
    string BlackName,
    string? Note = null);

/// <summary>
/// Backing view-model for the pre-commit pairing-preview dialog
/// (SwissSysFeatureParity #4 + #6 + #8 UI). Holds the working
/// <see cref="Tournament"/> and exposes intent methods that delegate to
/// the matching <see cref="TournamentMutations"/> calls and refresh the
/// row list. The dialog's code-behind binds to <see cref="Rows"/> and
/// <see cref="Conflicts"/>; on Accept it returns
/// <see cref="Tournament"/>; on Cancel the host reverts via
/// <see cref="TournamentMutations.DeleteLastRound"/>.
/// </summary>
public sealed partial class PairingPreviewViewModel : ObservableObject
{
    /// <summary>Section in which the preview round was just appended.</summary>
    public string SectionName { get; }

    /// <summary>1-based round number being previewed.</summary>
    public int Round { get; }

    /// <summary>
    /// Working tournament instance. Each TD action replaces this with
    /// the result of the corresponding mutation; <see cref="Rows"/>
    /// re-renders from the new state.
    /// </summary>
    public Tournament Tournament { get; private set; }

    /// <summary>Pre-formatted board rows for the dialog ItemsControl.</summary>
    public ObservableCollection<PairingPreviewRow> Rows { get; } = new();

    /// <summary>
    /// Human-readable warnings surfaced from
    /// <see cref="Bbp.BbpPairingResult.UnresolvedConflicts"/> (same-team /
    /// same-club / do-not-pair violations the post-BBP swapper couldn't
    /// resolve).
    /// </summary>
    public IReadOnlyList<string> Conflicts { get; }

    public PairingPreviewViewModel(
        Tournament tournament,
        string sectionName,
        int round,
        IReadOnlyList<string> conflicts)
    {
        Tournament = tournament;
        SectionName = sectionName;
        Round = round;
        Conflicts = conflicts ?? System.Array.Empty<string>();
        RefreshRows();
    }

    private void RefreshRows()
    {
        Rows.Clear();

        var section = Tournament.Sections.FirstOrDefault(s => s.Name == SectionName);
        var round = section?.Rounds.FirstOrDefault(r => r.Number == Round);
        if (section is null || round is null) return;

        var byPair = section.Players.ToDictionary(p => p.PairNumber);
        foreach (var p in round.Pairings.OrderBy(x => x.Board))
        {
            byPair.TryGetValue(p.WhitePair, out var w);
            byPair.TryGetValue(p.BlackPair, out var b);
            Rows.Add(new PairingPreviewRow(
                Board: p.Board,
                WhitePair: p.WhitePair,
                WhiteName: w?.Name ?? $"#{p.WhitePair}",
                BlackPair: p.BlackPair,
                BlackName: b?.Name ?? $"#{p.BlackPair}",
                Note: p.Note));
        }
    }

    /// <summary>
    /// Swaps colours on the pairing currently shown at
    /// <paramref name="row"/>. The white player becomes black and
    /// vice-versa.
    /// </summary>
    public void SwapColors(PairingPreviewRow row)
    {
        Tournament = TournamentMutations.SwapPairingColors(
            Tournament, SectionName, Round, row.WhitePair, row.BlackPair);
        RefreshRows();
    }

    /// <summary>
    /// Swaps the blacks across boards <paramref name="boardA"/> and
    /// <paramref name="boardB"/> (preserves colours; refuses to
    /// recreate a previously-played game). Throws via
    /// <see cref="TournamentMutations.SwapBoardOpponents"/> when the
    /// swap is illegal — the dialog should catch and surface the
    /// message.
    /// </summary>
    public void SwapBoards(int boardA, int boardB)
    {
        Tournament = TournamentMutations.SwapBoardOpponents(
            Tournament, SectionName, Round, boardA, boardB);
        RefreshRows();
    }

    /// <summary>
    /// Force-variant of <see cref="SwapBoards(int, int)"/>: skips
    /// the rematch guard and adds a session-only
    /// <see cref="Pairing.Note"/> to both updated pairings flagging
    /// the violation. Used by the drag-and-drop UI when the TD
    /// confirms a "yes, swap anyway" prompt.
    /// </summary>
    public void SwapBoardsForced(int boardA, int boardB)
    {
        Tournament = TournamentMutations.SwapBoardOpponents(
            Tournament, SectionName, Round, boardA, boardB, force: true);
        RefreshRows();
    }

    /// <summary>
    /// Cross-colour position swap (one player from each board
    /// trades places, colours flip). Always allowed; resulting
    /// rematches are flagged as session notes on both pairings
    /// instead of throwing. Drives the cross-colour drag-and-drop
    /// path in the preview dialog.
    /// </summary>
    public void SwapPlayerPositions(
        int sourceBoard, FreePair.Core.SwissSys.PlayerColor sourceColor,
        int targetBoard, FreePair.Core.SwissSys.PlayerColor targetColor)
    {
        Tournament = TournamentMutations.SwapPlayerPositions(
            Tournament, SectionName, Round,
            sourceBoard, sourceColor,
            targetBoard, targetColor);
        RefreshRows();
    }

    /// <summary>
    /// Converts the pairing at <paramref name="row"/> into a half-point
    /// bye for <paramref name="halfByePair"/> (must be one of the row's
    /// two players). Their opponent receives a full-point bye and the
    /// pairing disappears from the preview list.
    /// </summary>
    public void ConvertHalfBye(PairingPreviewRow row, int halfByePair)
    {
        Tournament = TournamentMutations.ConvertPairingToHalfPointBye(
            Tournament, SectionName, Round, halfByePair);
        RefreshRows();
    }
}
