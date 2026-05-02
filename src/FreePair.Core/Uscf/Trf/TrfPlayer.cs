using System.Collections.Generic;

namespace FreePair.Core.Uscf.Trf;

/// <summary>
/// One <c>001</c> player line from a TRF document.
/// </summary>
/// <param name="PairNumber">Starting rank / pair number (TRF cols 5-8).</param>
/// <param name="Name">Player's display name (cols 15-47, trimmed).</param>
/// <param name="Rating">Numeric rating (cols 49-52). 0 when blank.</param>
/// <param name="Id">USCF / FIDE ID (cols 58-68, trimmed). Empty when blank.</param>
/// <param name="Points">
/// Score reported by the writing tool (cols 81-84). The pairer recomputes
/// this from <see cref="Rounds"/> rather than trusting the stored value, so
/// this is informational only.
/// </param>
/// <param name="Rounds">
/// One cell per played round. Position N is the player's record from round
/// N+1 (1-indexed). Cells with no opponent represent unpaired / not-yet-
/// played rounds; cells with kind <c>H</c>/<c>U</c>/<c>F</c> etc. carry
/// the corresponding bye / forfeit.
/// </param>
public sealed record TrfPlayer(
    int PairNumber,
    string Name,
    int Rating,
    string Id,
    decimal Points,
    IReadOnlyList<TrfRoundCell> Rounds);
