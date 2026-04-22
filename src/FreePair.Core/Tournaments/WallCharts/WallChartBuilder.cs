using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments.Tiebreaks;

namespace FreePair.Core.Tournaments.WallCharts;

/// <summary>
/// Builds a section's wall chart — an ordered list of <see cref="WallChartRow"/>
/// objects, one per player, each with a compact round-by-round history plus
/// precomputed score and tiebreaks.
/// </summary>
public static class WallChartBuilder
{
    /// <summary>
    /// Projects a <see cref="Section"/> into wall-chart rows, one per player
    /// in ascending pair-number order. Each row contains exactly
    /// <see cref="Section.RoundsPlayed"/> cells.
    /// </summary>
    public static IReadOnlyList<WallChartRow> Build(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (section.Players.Count == 0)
        {
            return Array.Empty<WallChartRow>();
        }

        var tiebreaks = TiebreakCalculator.Compute(section);
        var rounds = section.RoundsPlayed;

        return section.Players
            .OrderBy(p => p.PairNumber)
            .Select(p => BuildRow(p, rounds, tiebreaks[p.PairNumber]))
            .ToArray();
    }

    /// <summary>
    /// Formats a single <see cref="RoundResult"/> into its compact wall-chart
    /// cell code. Exposed for testing and ad-hoc formatting outside the
    /// full-row builder.
    /// </summary>
    public static string FormatCell(RoundResult result)
    {
        switch (result.Kind)
        {
            case RoundResultKind.Win:
                return FormatPlayedGame("W", result);
            case RoundResultKind.Loss:
                return FormatPlayedGame("L", result);
            case RoundResultKind.Draw:
                return FormatPlayedGame("D", result);
            case RoundResultKind.FullPointBye:
                return "B---";
            case RoundResultKind.HalfPointBye:
                return "H---";
            case RoundResultKind.None:
                return "U---";
            default:
                return "----";
        }
    }

    private static WallChartRow BuildRow(Player player, int rounds, TiebreakValues tiebreaks)
    {
        var cells = new List<WallChartCell>(rounds);

        for (var r = 0; r < rounds; r++)
        {
            var res = r < player.History.Count ? player.History[r] : RoundResult.Empty;
            var opponent = res.Opponent > 0 ? res.Opponent : (int?)null;

            cells.Add(new WallChartCell(
                Round: r + 1,
                Kind: res.Kind,
                Opponent: opponent,
                Color: res.Color,
                Code: FormatCell(res)));
        }

        return new WallChartRow(
            PairNumber: player.PairNumber,
            Name: player.Name,
            Rating: player.Rating,
            Club: player.Club,
            State: player.State,
            Team: player.Team,
            Cells: cells,
            Score: player.Score,
            Tiebreaks: tiebreaks,
            Prize: null);
    }

    private static string FormatPlayedGame(string prefix, RoundResult result)
    {
        var sb = new StringBuilder(prefix.Length + 5);
        sb.Append(prefix);

        if (result.Opponent > 0)
        {
            sb.Append(result.Opponent.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append("--");
        }

        sb.Append(FormatColor(result.Color));
        return sb.ToString();
    }

    private static char FormatColor(PlayerColor color) => color switch
    {
        PlayerColor.White => 'W',
        PlayerColor.Black => 'B',
        _ => '-',
    };
}
