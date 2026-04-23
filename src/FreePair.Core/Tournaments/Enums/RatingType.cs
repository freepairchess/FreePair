namespace FreePair.Core.Tournaments.Enums;

/// <summary>
/// Rating federation(s) governing the event. Values mirror
/// NAChessHub's <c>RatingType</c> enum numerically, including the
/// combinatorial values (e.g. <see cref="USCF_FIDE"/>) used for
/// dual-rated events.
/// </summary>
/// <remarks>
/// Combinatorial entries are kept as single enum values (not [Flags])
/// to preserve bit-for-bit parity with the upstream enum. If we ever
/// need flag-style composition we can layer a mapper on top.
/// </remarks>
public enum RatingType
{
    UnRated = 0,
    USCF = 1,
    FIDE = 2,
    USCF_FIDE = 3,
    CHESSCOM = 4,
    LICHESS = 5,
    CHESS24 = 6,
    USCFONLINE = 7,
    CFC = 10,
    CFC_FIDE = 11,
    CFC_USCF = 12,
    CFC_USCF_FIDE = 13,
    USCF_NW = 14,
    USCF_FIDE_NW = 15,
    CFC_FIDE_NW = 16,
    USCF_CFC_FIDE_NW = 17,
    Other = 99,
}
