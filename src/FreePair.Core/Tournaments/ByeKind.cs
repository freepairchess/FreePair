namespace FreePair.Core.Tournaments;

/// <summary>
/// Kind of bye (or unpaired slot) assigned to a player in a given round.
/// </summary>
public enum ByeKind
{
    /// <summary>Full-point bye — 1.0 point, TD-assigned.</summary>
    Full,

    /// <summary>Half-point bye — 0.5 points, usually requested by the player.</summary>
    Half,

    /// <summary>
    /// Player was unpaired for this round (odd count, withdrawal, late entry,
    /// etc.). Scores 0 unless separately adjusted.
    /// </summary>
    Unpaired,
}
