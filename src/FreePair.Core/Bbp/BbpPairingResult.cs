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
public sealed record BbpPairingResult(
    IReadOnlyList<BbpPairing> Pairings,
    IReadOnlyList<int> ByePlayerPairs,
    IReadOnlyList<int>? HalfPointByePlayerPairs = null)
{
    /// <summary>
    /// Non-null view of <see cref="HalfPointByePlayerPairs"/>.
    /// </summary>
    public IReadOnlyList<int> HalfPointByes =>
        HalfPointByePlayerPairs ?? System.Array.Empty<int>();
}
