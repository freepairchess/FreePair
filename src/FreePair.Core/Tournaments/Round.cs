using System.Collections.Generic;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Reconstructed view of a single tournament round.
/// </summary>
public sealed record Round(
    int Number,
    IReadOnlyList<Pairing> Pairings,
    IReadOnlyList<ByeAssignment> Byes);
