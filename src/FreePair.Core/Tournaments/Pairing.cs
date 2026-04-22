namespace FreePair.Core.Tournaments;

/// <summary>
/// A played (or scheduled) board pairing between two players.
/// </summary>
public sealed record Pairing(
    int Board,
    int WhitePair,
    int BlackPair,
    PairingResult Result);
