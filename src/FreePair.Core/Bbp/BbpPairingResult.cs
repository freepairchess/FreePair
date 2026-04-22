using System.Collections.Generic;

namespace FreePair.Core.Bbp;

/// <summary>
/// The structured result of running the BBP pairing engine for one round:
/// the set of paired games plus the players (if any) receiving a bye.
/// </summary>
public sealed record BbpPairingResult(
    IReadOnlyList<BbpPairing> Pairings,
    IReadOnlyList<int> ByePlayerPairs);
