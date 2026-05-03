using System;
using System.Collections.Generic;

namespace FreePair.Core.Uscf;

/// <summary>
/// One pre-flagged bye honoured by the pairer (half-point or zero-point).
/// Distinct from the auto-assigned full-point bye which lives in
/// <see cref="UscfPairingResult.ByePair"/>.
/// </summary>
/// <param name="PairNumber">Pair number of the player receiving the bye.</param>
/// <param name="Kind">
/// <c>'H'</c> for half-point bye, <c>'Z'</c> for zero-point / unpaired.
/// </param>
public sealed record UscfRequestedBye(int PairNumber, char Kind);

/// <summary>
/// Output of <see cref="UscfPairer.Pair"/>: the pairings for the next round
/// in board order, plus an optional bye player when the round has an odd
/// number of pairable players.
/// </summary>
/// <param name="Pairings">Pairings in board-number order.</param>
/// <param name="ByePair">
/// Pair number of the player who received the round's full-point bye, or
/// <c>null</c> when the round has no bye.
/// </param>
/// <param name="RequestedByes">
/// Pre-flagged half- and zero-point byes the pairer honoured by excluding
/// those players from the pairing pool. Empty when no requests were
/// supplied via <see cref="Trf.TrfDocument.RequestedByes"/>.
/// </param>
public sealed record UscfPairingResult(
    IReadOnlyList<UscfPairing> Pairings,
    int? ByePair,
    IReadOnlyList<UscfRequestedBye>? RequestedByes = null)
{
    /// <summary>Non-null view of <see cref="RequestedByes"/> for callers that prefer enumeration.</summary>
    public IReadOnlyList<UscfRequestedBye> RequestedByesOrEmpty =>
        RequestedByes ?? Array.Empty<UscfRequestedBye>();
}
