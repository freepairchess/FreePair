using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FreePair.Core.Tournaments.Tiebreaks;

namespace FreePair.Core.Tournaments.Standings;

/// <summary>
/// Builds the final standings for a <see cref="Section"/>, sorted by score
/// and tiebreaks, and assigns USCF-style place labels where equally-scored
/// players share a place range (e.g. <c>"3-5"</c>).
/// </summary>
/// <remarks>
/// <para>Sort order (all descending except the final key):</para>
/// <list type="number">
///   <item>Score</item>
///   <item>Modified Median</item>
///   <item>Solkoff</item>
///   <item>Cumulative</item>
///   <item>Opponent Cumulative</item>
///   <item>Pair number (ascending, as a stable final key)</item>
/// </list>
/// <para>Place labels group by <b>score only</b>: tied-on-score players
/// share a <c>"first-last"</c> range even when tiebreaks separate them
/// within the group.</para>
/// </remarks>
public static class StandingsBuilder
{
    /// <summary>Builds ordered standings rows for the given section.</summary>
    public static IReadOnlyList<StandingsRow> Build(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (section.Players.Count == 0)
        {
            return Array.Empty<StandingsRow>();
        }

        var tiebreaks = TiebreakCalculator.Compute(section);

        var sorted = section.Players
            .Select(p => new SortItem(p, tiebreaks[p.PairNumber]))
            .OrderByDescending(x => x.Player.Score)
            .ThenByDescending(x => x.Tb.ModifiedMedian)
            .ThenByDescending(x => x.Tb.Solkoff)
            .ThenByDescending(x => x.Tb.Cumulative)
            .ThenByDescending(x => x.Tb.OpponentCumulative)
            .ThenBy(x => x.Player.PairNumber)
            .ToArray();

        var rows = new List<StandingsRow>(sorted.Length);
        var i = 0;

        while (i < sorted.Length)
        {
            var groupScore = sorted[i].Player.Score;
            var j = i + 1;
            while (j < sorted.Length && sorted[j].Player.Score == groupScore)
            {
                j++;
            }

            var firstRank = i + 1;
            var lastRank = j;
            var place = firstRank == lastRank
                ? firstRank.ToString(CultureInfo.InvariantCulture)
                : $"{firstRank}-{lastRank}";

            for (var k = i; k < j; k++)
            {
                var item = sorted[k];
                rows.Add(new StandingsRow(
                    Rank: k + 1,
                    Place: place,
                    PairNumber: item.Player.PairNumber,
                    Name: item.Player.Name,
                    Rating: item.Player.Rating,
                    Score: item.Player.Score,
                    Tiebreaks: item.Tb));
            }

            i = j;
        }

        return rows;
    }

    private readonly record struct SortItem(Player Player, TiebreakValues Tb);
}
