namespace FreePair.Core.Tournaments;

/// <summary>
/// A TD-specified pre-pairing that locks two players onto a board in
/// the given round before the pairing engine runs. The affected
/// players are withheld from the TRF sent to bbpPairings (or from the
/// <see cref="RoundRobinScheduler"/>'s input pool) so the engine never
/// tries to pair them; the fixed pairing is then prepended to the
/// engine's output, taking board 1 (and the subsequent boards when
/// multiple forced pairings exist for the same round).
/// </summary>
/// <param name="Round">
/// One-based round number in which the pairing is enforced. A single
/// event can carry forced pairings for multiple rounds; the engine
/// only considers the ones whose <see cref="Round"/> equals the round
/// about to be paired.
/// </param>
/// <param name="WhitePair">Pair number receiving white.</param>
/// <param name="BlackPair">Pair number receiving black.</param>
public sealed record ForcedPairing(int Round, int WhitePair, int BlackPair);
