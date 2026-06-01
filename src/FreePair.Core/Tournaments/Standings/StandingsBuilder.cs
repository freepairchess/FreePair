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

        // Only count scores through completed rounds (RoundsPlayed).
        // In-progress round results are excluded so standings don't
        // fluctuate while the round is being entered.
        var roundsPlayed = section.RoundsPlayed;

        // Soft-deleted players (pre-round-1 only) are omitted from the
        // standings entirely — they don't appear in FreePair's own
        // standings / wall-chart views, the TRF export, or the
        // NA Chess Hub publish payload.
        var sorted = section.Players
            .Where(p => !p.SoftDeleted)
            .Select(p => new SortItem(p, tiebreaks[p.PairNumber], ScoreThrough(p, roundsPlayed)))
            .OrderByDescending(x => x.Score)
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
            var groupScore = sorted[i].Score;
            var j = i + 1;
            while (j < sorted.Length && sorted[j].Score == groupScore)
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
                    Place: k == i ? place : string.Empty,
                    PairNumber: item.Player.PairNumber,
                    Name: item.Player.Name,
                    Rating: item.Player.Rating,
                    Score: item.Score,
                    Tiebreaks: item.Tb));
            }

            i = j;
        }

        return rows;
    }

    private readonly record struct SortItem(Player Player, TiebreakValues Tb, decimal Score);

    private static decimal ScoreThrough(Player player, int roundsPlayed)
    {
        if (roundsPlayed <= 0) return 0m;
        var rounds = Math.Min(roundsPlayed, player.History.Count);
        decimal score = 0m;
        for (var i = 0; i < rounds; i++) score += player.History[i].Score;
        return score;
    }
}
