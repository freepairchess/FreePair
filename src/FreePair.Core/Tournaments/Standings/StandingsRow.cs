using FreePair.Core.Tournaments.Tiebreaks;

namespace FreePair.Core.Tournaments.Standings;

/// <summary>
/// A single row of a section's final standings, combining score, tiebreaks,
/// and the computed place label (supports ties, e.g. <c>"3-5"</c>).
/// </summary>
public sealed record StandingsRow(
    int Rank,
    string Place,
    int PairNumber,
    string Name,
    int Rating,
    decimal Score,
    TiebreakValues Tiebreaks);
