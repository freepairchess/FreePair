using FreePair.Core.Tournaments.Standings;
using FreePair.Core.Tournaments.WallCharts;

namespace FreePair.App.ViewModels;

/// <summary>
/// Wraps a <see cref="StandingsRow"/> with pre-formatted display strings for
/// score and tiebreaks, so the view can bind directly to ASCII / Unicode
/// output controlled by the app's <see cref="FreePair.Core.Formatting.IScoreFormatter"/>.
/// </summary>
public sealed record StandingsDisplayRow(
    StandingsRow Row,
    string ScoreText,
    string ModMedText,
    string SolkoffText,
    string CumulativeText,
    string OppCumulativeText);

/// <summary>
/// Wraps a <see cref="WallChartRow"/> with pre-formatted display strings.
/// </summary>
public sealed record WallChartDisplayRow(
    WallChartRow Row,
    string ScoreText,
    string ModMedText,
    string SolkoffText,
    string CumulativeText,
    string OppCumulativeText);
