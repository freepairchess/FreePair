using System;
using System.Linq;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Computed pairing-readiness status for one section, used by the
/// "Pair all sections" dialog to decide which sections can be paired
/// in a batch and which should be skipped with an explanation.
/// Pure function over <see cref="Section"/>; no UI / IO.
/// </summary>
public enum SectionPairingStatus
{
    /// <summary>
    /// Section can be paired right now: at least 2 active players,
    /// the previously paired round (if any) is complete, and not
    /// all <see cref="Section.FinalRound"/> rounds have been paired.
    /// </summary>
    ReadyToPair = 0,

    /// <summary>
    /// The most recently paired round still has pairings without
    /// a final result. Pair-all skips this section so the TD enters
    /// results first.
    /// </summary>
    WaitingForResults,

    /// <summary>
    /// Every planned round has been paired already; nothing more to
    /// do until <see cref="Section.FinalRound"/> is bumped.
    /// </summary>
    AllRoundsPaired,

    /// <summary>
    /// Fewer than two active (non-soft-deleted, non-withdrawn)
    /// players — BBP can't pair a section with one or zero.
    /// </summary>
    InsufficientPlayers,

    /// <summary>
    /// Section is soft-deleted; pair-all hides it entirely.
    /// </summary>
    SoftDeleted,
}

/// <summary>
/// Static helpers to compute <see cref="SectionPairingStatus"/> and
/// associated context (next round number, active player count) used
/// by the Pair-all-sections dialog.
/// </summary>
public static class SectionPairingReadiness
{
    /// <summary>
    /// Classifies <paramref name="section"/>'s pairing readiness.
    /// Order of checks (first matching wins): SoftDeleted →
    /// InsufficientPlayers → AllRoundsPaired → WaitingForResults →
    /// ReadyToPair.
    /// </summary>
    public static SectionPairingStatus Classify(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (section.SoftDeleted) return SectionPairingStatus.SoftDeleted;

        var activeCount = ActivePlayerCount(section);
        if (activeCount < 2) return SectionPairingStatus.InsufficientPlayers;

        if (section.Rounds.Count >= section.FinalRound && section.FinalRound > 0)
            return SectionPairingStatus.AllRoundsPaired;

        if (section.Rounds.Count > 0)
        {
            var last = section.Rounds[section.Rounds.Count - 1];
            if (last.Pairings.Any(p => p.Result == PairingResult.Unplayed))
            {
                return SectionPairingStatus.WaitingForResults;
            }
        }

        return SectionPairingStatus.ReadyToPair;
    }

    /// <summary>
    /// Active player count = non-soft-deleted, non-withdrawn. Same
    /// definition <see cref="BoardNumberRecommender"/> uses so the
    /// Pair-all dialog and the Renumber-boards dialog agree on
    /// numbers.
    /// </summary>
    public static int ActivePlayerCount(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return section.Players.Count(p => !p.SoftDeleted && !p.Withdrawn);
    }

    /// <summary>
    /// Round number that would be paired next — one past the most
    /// recent already-paired round (1-based).
    /// </summary>
    public static int NextRoundNumber(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return section.Rounds.Count + 1;
    }
}
