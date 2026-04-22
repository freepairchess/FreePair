using System.Collections.Generic;
using FreePair.Core.Tournaments.Tiebreaks;

namespace FreePair.Core.Tournaments.WallCharts;

/// <summary>
/// A single row in a section wall chart — one player, all of their rounds
/// collapsed into compact cells plus score and tiebreaks.
/// </summary>
public sealed record WallChartRow(
    int PairNumber,
    string Name,
    int Rating,
    string? Club,
    string? State,
    string? Team,
    IReadOnlyList<WallChartCell> Cells,
    decimal Score,
    TiebreakValues Tiebreaks,
    decimal? Prize);
