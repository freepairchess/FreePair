using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Correctness tests for <see cref="RoundRobinScheduler"/>:
/// <list type="bullet">
///   <item>Every distinct pair meets exactly once.</item>
///   <item>Round count is <c>N-1</c> for even <c>N</c>, <c>N</c> for odd.</item>
///   <item>Every player plays exactly one game per round (or is byed).</item>
///   <item>Colour split is balanced to within one game per player.</item>
/// </list>
/// </summary>
public class RoundRobinSchedulerTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    public void Every_pair_meets_exactly_once(int n)
    {
        var seats = Enumerable.Range(1, n).ToArray();
        var rounds = RoundRobinScheduler.Build(seats);

        // Collect every pair (unordered) across all rounds.
        var seen = new HashSet<(int Lo, int Hi)>();
        foreach (var round in rounds)
        {
            foreach (var p in round.Pairings)
            {
                var lo = System.Math.Min(p.WhitePair, p.BlackPair);
                var hi = System.Math.Max(p.WhitePair, p.BlackPair);
                Assert.True(seen.Add((lo, hi)), $"Duplicate pairing: {lo} vs {hi}");
            }
        }

        // Count expected meetings = C(n, 2).
        Assert.Equal(n * (n - 1) / 2, seen.Count);
    }

    [Theory]
    [InlineData(4, 3)]  // even N → N-1 rounds
    [InlineData(6, 5)]
    [InlineData(8, 7)]
    [InlineData(3, 3)]  // odd  N → N rounds (each gets one bye)
    [InlineData(5, 5)]
    [InlineData(7, 7)]
    public void Round_count_matches_standard(int n, int expectedRounds)
    {
        var seats = Enumerable.Range(1, n).ToArray();
        var rounds = RoundRobinScheduler.Build(seats);
        Assert.Equal(expectedRounds, rounds.Count);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    public void Odd_field_byes_each_player_exactly_once(int n)
    {
        var seats = Enumerable.Range(1, n).ToArray();
        var rounds = RoundRobinScheduler.Build(seats);

        var byCounts = seats.ToDictionary(p => p, _ => 0);
        foreach (var round in rounds)
        {
            foreach (var b in round.Byes)
            {
                byCounts[b.PlayerPair]++;
            }
        }

        Assert.All(byCounts.Values, c => Assert.Equal(1, c));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void Even_field_has_no_byes(int n)
    {
        var seats = Enumerable.Range(1, n).ToArray();
        var rounds = RoundRobinScheduler.Build(seats);
        Assert.All(rounds, r => Assert.Empty(r.Byes));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(10)]
    public void Colour_split_is_balanced_to_within_one_game(int n)
    {
        var seats = Enumerable.Range(1, n).ToArray();
        var rounds = RoundRobinScheduler.Build(seats);

        var whiteCounts = seats.ToDictionary(p => p, _ => 0);
        var blackCounts = seats.ToDictionary(p => p, _ => 0);

        foreach (var round in rounds)
        {
            foreach (var p in round.Pairings)
            {
                whiteCounts[p.WhitePair]++;
                blackCounts[p.BlackPair]++;
            }
        }

        foreach (var player in seats)
        {
            var delta = System.Math.Abs(whiteCounts[player] - blackCounts[player]);
            Assert.True(delta <= 1,
                $"Player {player}: W={whiteCounts[player]} B={blackCounts[player]} delta={delta}");
        }
    }

    [Fact]
    public void Empty_or_single_player_field_throws()
    {
        Assert.Throws<System.ArgumentException>(() =>
            RoundRobinScheduler.Build(System.Array.Empty<int>()));
        Assert.Throws<System.ArgumentException>(() =>
            RoundRobinScheduler.Build(new[] { 1 }));
    }

    [Fact]
    public void Duplicate_pair_numbers_throw()
    {
        Assert.Throws<System.ArgumentException>(() =>
            RoundRobinScheduler.Build(new[] { 1, 2, 2, 3 }));
    }
}
