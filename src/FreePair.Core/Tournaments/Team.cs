using System.Collections.Generic;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments;

/// <summary>
/// A team entry in a team-enabled section.
/// </summary>
public sealed record Team(
    int PairNumber,
    string Name,
    string? Code,
    int Rating,
    IReadOnlyList<RoundResult> History);
