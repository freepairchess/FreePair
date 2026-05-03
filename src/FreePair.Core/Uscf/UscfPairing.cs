namespace FreePair.Core.Uscf;

/// <summary>
/// One pairing produced by <see cref="UscfPairer"/>. <see cref="WhitePair"/>
/// and <see cref="BlackPair"/> are TRF starting-rank / pair numbers.
/// A bye is represented by the bye player in <see cref="WhitePair"/> and
/// <c>0</c> in <see cref="BlackPair"/>.
/// </summary>
public sealed record UscfPairing(int WhitePair, int BlackPair, int Board);
