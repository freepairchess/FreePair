using System.Collections.Generic;

namespace FreePair.Core.Bbp;

/// <summary>
/// The structured result of running the BBP pairing engine for one round:
/// the set of paired games plus the players (if any) receiving a bye.
/// </summary>
/// <param name="Pairings">
/// The boards BBP produced for the round (white/black pair numbers).
/// </param>
/// <param name="ByePlayerPairs">
/// Pair numbers that BBP <em>assigned</em> a full-point bye to — typically
/// the odd-player-out when the section has an odd number of active
/// players for the round.
/// </param>
/// <param name="HalfPointByePlayerPairs">
/// Pair numbers that were <em>pre-flagged</em> for a half-point bye via
/// the TRF (e.g. a TD-granted requested bye). BBP sees the 'H' cell and
/// skips pairing these players; this list lets
/// <see cref="Tournaments.TournamentMutations.AppendRound"/> stamp the
/// correct <c>HalfPointBye</c> history entry so standings and the wall
/// chart reflect the half-point. Empty when no requested byes apply.
/// </param>
/// <param name="UnresolvedConflicts">
/// Human-readable reasons for any pairing-constraint violations (same
/// team / same club / do-not-pair list) that the post-BBP
/// <see cref="Tournaments.PairingSwapper"/> couldn't resolve via a
/// same-score-group swap. Empty when every proposed pairing cleared
/// the active constraints. Surface this to the TD as a warning so
/// they can decide whether to accept or manually intervene.
/// </param>
public sealed record BbpPairingResult(
    IReadOnlyList<BbpPairing> Pairings,
    IReadOnlyList<int> ByePlayerPairs,
    IReadOnlyList<int>? HalfPointByePlayerPairs = null,
    IReadOnlyList<string>? UnresolvedConflicts = null,
    /// <summary>
    /// Pair numbers pre-flagged for a zero-point bye via
    /// <see cref="Tournaments.Player.ZeroPointByeRounds"/>. Like the
    /// half-point variant, BBP never sees these players (they're
    /// filtered out of the active roster before TRF generation); the
    /// result surfaces them so <see cref="Tournaments.TournamentMutations.AppendRound"/>
    /// can stamp a <c>ZeroPointBye</c> history entry.
    /// </summary>
    IReadOnlyList<int>? ZeroPointByePlayerPairs = null)
{
    /// <summary>
    /// Non-null view of <see cref="HalfPointByePlayerPairs"/>.
    /// </summary>
    public IReadOnlyList<int> HalfPointByes =>
        HalfPointByePlayerPairs ?? System.Array.Empty<int>();

    /// <summary>
    /// Non-null view of <see cref="UnresolvedConflicts"/>.
    /// </summary>
    public IReadOnlyList<string> Conflicts =>
        UnresolvedConflicts ?? System.Array.Empty<string>();

    /// <summary>
    /// Non-null view of <see cref="ZeroPointByePlayerPairs"/>.
    /// </summary>
    public IReadOnlyList<int> ZeroPointByes =>
        ZeroPointByePlayerPairs ?? System.Array.Empty<int>();
}
