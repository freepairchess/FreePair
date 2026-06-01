using System.Collections.Generic;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Reconstructed view of a single tournament round.
/// </summary>
public sealed record Round(
    int Number,
    IReadOnlyList<Pairing> Pairings,
    IReadOnlyList<ByeAssignment> Byes,
    IReadOnlyList<PairingAnnotation>? Annotations = null)
{
    /// <summary>Non-null view of <see cref="Annotations"/>.</summary>
    public IReadOnlyList<PairingAnnotation> AnnotationsOrEmpty =>
        Annotations ?? System.Array.Empty<PairingAnnotation>();
}
