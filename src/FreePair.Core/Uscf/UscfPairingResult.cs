using System.Collections.Generic;

namespace FreePair.Core.Uscf;

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
public sealed record UscfPairingResult(
    IReadOnlyList<UscfPairing> Pairings,
    int? ByePair);
