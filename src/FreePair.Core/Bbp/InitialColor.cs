namespace FreePair.Core.Bbp;

/// <summary>
/// Initial piece-colour assignment for the top seed on board 1 of round 1.
/// Maps to the TRF <c>XXC white1</c> / <c>XXC black1</c> directive that
/// pairing engines read to seed colour allocation for the opening round.
/// </summary>
public enum InitialColor
{
    White,
    Black,
}
