using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments.WallCharts;

/// <summary>
/// A single round cell in a wall chart row.
/// </summary>
/// <remarks>
/// <see cref="Code"/> contains the compact US-Chess-style representation
/// (e.g. <c>W9B</c>, <c>L4W</c>, <c>D16W</c>, <c>B---</c>, <c>H---</c>,
/// <c>U---</c>) suitable for direct display in a grid.
/// </remarks>
public sealed record WallChartCell(
    int Round,
    RoundResultKind Kind,
    int? Opponent,
    PlayerColor Color,
    string Code);
