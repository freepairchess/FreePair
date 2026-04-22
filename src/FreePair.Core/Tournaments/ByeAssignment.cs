namespace FreePair.Core.Tournaments;

/// <summary>
/// A bye or unpaired assignment for a single player in a single round.
/// </summary>
public sealed record ByeAssignment(int PlayerPair, ByeKind Kind);
