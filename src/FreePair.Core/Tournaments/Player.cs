using System.Collections.Generic;
using System.Linq;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments;

/// <summary>
/// A single player (or pair-number slot) in a tournament section.
/// </summary>
public sealed record Player(
    int PairNumber,
    string Name,
    string? UscfId,
    int Rating,
    int? SecondaryRating,
    string? MembershipExpiration,
    string? Club,
    string? State,
    string? Team,
    IReadOnlyList<int> RequestedByeRounds,
    IReadOnlyList<RoundResult> History)
{
    /// <summary>
    /// Sum of scoring results across <see cref="History"/> (1 per win / full
    /// bye, 0.5 per draw / half bye, 0 otherwise).
    /// </summary>
    public decimal Score => History.Sum(r => r.Score);
}
