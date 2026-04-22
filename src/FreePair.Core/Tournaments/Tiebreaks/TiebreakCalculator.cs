using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments.Tiebreaks;

/// <summary>
/// Computes the four standard USCF tiebreaks (Modified Median, Solkoff,
/// Cumulative, Opponent Cumulative) for players in a <see cref="Section"/>.
/// </summary>
/// <remarks>
/// <para>Rule summary (validated against SwissSys-generated oracle data):</para>
/// <list type="bullet">
///   <item>
///     <b>Solkoff:</b> sum over rounds of each opponent's <i>adjusted</i>
///     score, where every one of the opponent's own unplayed games
///     (byes / forfeits / unpaired) counts as 0.5 points. A round where
///     the player themselves was unpaired contributes 0 (no phantom
///     opponent).
///   </item>
///   <item>
///     <b>Modified Median:</b> same per-round contributions as Solkoff;
///     sorted ascending, then values are dropped based on the player's
///     own score vs. <c>roundsPlayed / 2</c>: above half → drop lowest,
///     below half → drop highest, equal to half → drop both extremes.
///   </item>
///   <item>
///     <b>Cumulative:</b> sum of the player's running score at each round,
///     minus the total points awarded by byes (full-point bye = 1,
///     half-point bye = 0.5). Equivalent to USCF rule 34E2.
///   </item>
///   <item>
///     <b>Opponent Cumulative:</b> sum of each opponent's Cumulative
///     value; rounds with no opponent contribute 0.
///   </item>
/// </list>
/// </remarks>
public static class TiebreakCalculator
{
    /// <summary>Computes all four tiebreaks for every player in a section.</summary>
    public static IReadOnlyDictionary<int, TiebreakValues> Compute(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var byPair = BuildLookup(section);
        var result = new Dictionary<int, TiebreakValues>(section.Players.Count);

        foreach (var player in section.Players)
        {
            result[player.PairNumber] = ComputeFor(section, player, byPair);
        }

        return result;
    }

    /// <summary>Computes all four tiebreaks for a single player.</summary>
    public static TiebreakValues ComputeFor(Section section, Player player)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(player);

        return ComputeFor(section, player, BuildLookup(section));
    }

    /// <summary>
    /// Cumulative tiebreak: running-sum minus bye adjustments (USCF 34E2).
    /// </summary>
    public static decimal Cumulative(Player player, int roundsPlayed)
    {
        ArgumentNullException.ThrowIfNull(player);

        var rounds = Math.Min(roundsPlayed, player.History.Count);
        if (rounds <= 0)
        {
            return 0m;
        }

        var running = 0m;
        var sum = 0m;
        var byeAdjustment = 0m;

        for (var r = 0; r < rounds; r++)
        {
            var res = player.History[r];
            running += res.Score;
            sum += running;

            if (res.Kind == RoundResultKind.FullPointBye)
            {
                byeAdjustment += 1m;
            }
            else if (res.Kind == RoundResultKind.HalfPointBye)
            {
                byeAdjustment += 0.5m;
            }
        }

        return sum - byeAdjustment;
    }

    /// <summary>Solkoff: sum of opponent-adjusted scores across all rounds.</summary>
    public static decimal Solkoff(Section section, Player player)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(player);

        return GetOpponentContributions(section, player, BuildLookup(section)).Sum();
    }

    /// <summary>
    /// Modified Median: sort opponent-adjusted scores, drop based on the
    /// player's score relative to half the rounds played.
    /// </summary>
    public static decimal ModifiedMedian(Section section, Player player)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(player);

        return ModifiedMedianCore(section, player, BuildLookup(section));
    }

    /// <summary>Opponent Cumulative: sum of each opponent's Cumulative score.</summary>
    public static decimal OpponentCumulative(Section section, Player player)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(player);

        return OpponentCumulativeCore(section, player, BuildLookup(section));
    }

    private static TiebreakValues ComputeFor(
        Section section,
        Player player,
        IReadOnlyDictionary<int, Player> byPair)
    {
        var contributions = GetOpponentContributions(section, player, byPair).ToArray();

        var solkoff = contributions.Sum();
        var modMed = ApplyMedianDrops(contributions, player.Score, section.RoundsPlayed);
        var cumul = Cumulative(player, section.RoundsPlayed);
        var opCumul = OpponentCumulativeCore(section, player, byPair);

        return new TiebreakValues(modMed, solkoff, cumul, opCumul);
    }

    private static Dictionary<int, Player> BuildLookup(Section section) =>
        section.Players.ToDictionary(p => p.PairNumber);

    /// <summary>
    /// Yields each round's opponent-score contribution to Solkoff / Modified
    /// Median. Player's own unplayed round contributes 0.
    /// </summary>
    private static IEnumerable<decimal> GetOpponentContributions(
        Section section,
        Player player,
        IReadOnlyDictionary<int, Player> byPair)
    {
        var rounds = Math.Min(section.RoundsPlayed, player.History.Count);
        for (var r = 0; r < rounds; r++)
        {
            var res = player.History[r];

            if (IsPlayedGame(res.Kind) &&
                res.Opponent > 0 &&
                byPair.TryGetValue(res.Opponent, out var opponent))
            {
                yield return AdjustedOpponentScore(opponent, rounds);
            }
            else
            {
                // Player's own bye / unpaired / uninitialized → no phantom opponent.
                yield return 0m;
            }
        }
    }

    /// <summary>
    /// Opponent's adjusted score used by Solkoff / Modified Median: each of
    /// the opponent's unplayed rounds (bye/forfeit/unpaired) counts as 0.5.
    /// </summary>
    private static decimal AdjustedOpponentScore(Player opponent, int roundsPlayed)
    {
        var rounds = Math.Min(roundsPlayed, opponent.History.Count);
        var sum = 0m;

        for (var r = 0; r < rounds; r++)
        {
            var res = opponent.History[r];
            sum += IsPlayedGame(res.Kind) ? res.Score : 0.5m;
        }

        return sum;
    }

    private static decimal ModifiedMedianCore(
        Section section,
        Player player,
        IReadOnlyDictionary<int, Player> byPair)
    {
        var contributions = GetOpponentContributions(section, player, byPair).ToArray();
        return ApplyMedianDrops(contributions, player.Score, section.RoundsPlayed);
    }

    private static decimal ApplyMedianDrops(
        IReadOnlyList<decimal> contributions,
        decimal playerScore,
        int roundsPlayed)
    {
        if (contributions.Count == 0 || roundsPlayed <= 0)
        {
            return 0m;
        }

        var sorted = contributions.OrderBy(x => x).ToList();
        var halfScore = roundsPlayed / 2m;

        if (playerScore > halfScore)
        {
            // Drop the single lowest contribution.
            if (sorted.Count > 0)
            {
                sorted.RemoveAt(0);
            }
        }
        else if (playerScore < halfScore)
        {
            // Drop the single highest contribution.
            if (sorted.Count > 0)
            {
                sorted.RemoveAt(sorted.Count - 1);
            }
        }
        else
        {
            // Exactly half: drop both extremes (classic Median).
            if (sorted.Count >= 2)
            {
                sorted.RemoveAt(sorted.Count - 1);
                sorted.RemoveAt(0);
            }
        }

        return sorted.Sum();
    }

    private static decimal OpponentCumulativeCore(
        Section section,
        Player player,
        IReadOnlyDictionary<int, Player> byPair)
    {
        var rounds = Math.Min(section.RoundsPlayed, player.History.Count);
        var sum = 0m;

        for (var r = 0; r < rounds; r++)
        {
            var res = player.History[r];
            if (IsPlayedGame(res.Kind) &&
                res.Opponent > 0 &&
                byPair.TryGetValue(res.Opponent, out var opponent))
            {
                sum += Cumulative(opponent, section.RoundsPlayed);
            }
        }

        return sum;
    }

    private static bool IsPlayedGame(RoundResultKind kind) =>
        kind is RoundResultKind.Win or RoundResultKind.Loss or RoundResultKind.Draw;
}
