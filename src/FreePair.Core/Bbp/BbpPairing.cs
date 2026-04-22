namespace FreePair.Core.Bbp;

/// <summary>
/// A single pairing for the next round as reported by the BBP pairing engine.
/// <see cref="WhitePair"/> / <see cref="BlackPair"/> reference the starting
/// rank (pair number) of the two players.
/// </summary>
public sealed record BbpPairing(int WhitePair, int BlackPair);
