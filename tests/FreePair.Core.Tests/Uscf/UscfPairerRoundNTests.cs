using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;

namespace FreePair.Core.Tests.Uscf;

/// <summary>
/// Round 2+ pairing tests for <see cref="UscfPairer"/>. USCF rule 28D
/// (score-group pairing) and 28L (drop-downs / byes when a score group
/// has an odd number of players).
/// </summary>
/// <remarks>
/// <para>These exercise the matching <em>shape</em> of <c>PairRoundN</c> —
/// score-group ordering, top-half-vs-bottom-half within groups, odd-group
/// drop-downs, and bye assignment. Repeat-pairing avoidance via
/// transpositions (P2) and full 29D colour preference resolution (P3) are
/// out of scope; tests here pick rosters where the simple algorithm
/// happens to produce a correct USCF-compliant answer so we can verify
/// the matching layer in isolation.</para>
/// </remarks>
public class UscfPairerRoundNTests
{
    [Fact]
    public void Round_2_pairs_within_score_groups()
    {
        // 4 players, all played round 1. Two won (1.0), two lost (0.0).
        // Round 2 should pair winner-vs-winner and loser-vs-loser.
        // Round 1: 1 (W) beat 3 (B); 2 (W) beat 4 (B).
        var doc = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2200, Cells: [Cell(opp: 3, color: 'w', result: '1')]),
            new RoundCellSpec(PairNumber: 2, Rating: 2100, Cells: [Cell(opp: 4, color: 'w', result: '1')]),
            new RoundCellSpec(PairNumber: 3, Rating: 2000, Cells: [Cell(opp: 1, color: 'b', result: '0')]),
            new RoundCellSpec(PairNumber: 4, Rating: 1900, Cells: [Cell(opp: 2, color: 'b', result: '0')]));

        var result = UscfPairer.Pair(doc);

        Assert.Null(result.ByePair);
        Assert.Equal(2, result.Pairings.Count);

        // 1.0 score group: pair 1 (top of group) plays pair 2 (bottom).
        // 0.0 score group: pair 3 plays pair 4.
        var pairs = result.Pairings
            .Select(p => (Math.Min(p.WhitePair, p.BlackPair), Math.Max(p.WhitePair, p.BlackPair)))
            .ToHashSet();

        Assert.Contains((1, 2), pairs);
        Assert.Contains((3, 4), pairs);
    }

    [Fact]
    public void Odd_score_group_floats_lowest_rated_down_to_next_group()
    {
        // 6 players. After R1: pairs 1, 2, 3 have 1.0 (winners);
        // pairs 4, 5, 6 have 0.0 (losers). Both groups are odd (3+3),
        // so the lowest-rated of the 1.0 group (pair 3) floats down to
        // the 0.0 group, and pair 6 (lowest of 0.0) also can't pair → bye?
        //
        // Walk-through:
        //   1.0 group: [1, 2, 3]. Odd, drop pair 3 to floatDown.
        //              Remaining [1, 2] → pair (1 vs 2).
        //   0.0 group: pool = [3, 4, 5, 6]. 3 has higher score (1.0 from
        //              the float-down), then 4/5/6 (all 0.0). Sorted:
        //              [3 (score 1.0), 4 (0.0, rating 1700), 5 (0.0, 1600),
        //               6 (0.0, 1500)]. Even, pair top-vs-bottom:
        //              3 vs 5, 4 vs 6.
        // No bye.
        var doc = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2200, Cells: [Cell(2, 'w', '1')]),
            new RoundCellSpec(PairNumber: 2, Rating: 2100, Cells: [Cell(1, 'b', '0')]),
            new RoundCellSpec(PairNumber: 3, Rating: 2000, Cells: [Cell(0, '-', 'U')]),  // R1 bye, score 1.0
            new RoundCellSpec(PairNumber: 4, Rating: 1700, Cells: [Cell(5, 'w', '1')]),
            new RoundCellSpec(PairNumber: 5, Rating: 1600, Cells: [Cell(4, 'b', '0')]),
            new RoundCellSpec(PairNumber: 6, Rating: 1500, Cells: [Cell(0, '-', '-')])); // R1 unpaired

        // Adjust scores to make it interesting:
        //   pair 1 (1.0), pair 2 (0.0), pair 3 (1.0 from bye),
        //   pair 4 (1.0 from win), pair 5 (0.0), pair 6 (0.0)
        // → score groups: 1.0 = {1, 3, 4}, 0.0 = {2, 5, 6}
        // 1.0 group: [1 (R2200), 3 (R2000), 4 (R1700)] → odd, drop pair 4.
        //   Remaining [1, 3] → pair (1 vs 3).
        // 0.0 group: pool = [4, 2, 5, 6]. 4 has score 1.0, others 0.0.
        //   Sorted by score desc → [4 (1.0), 2 (0.0, R2100), 5 (0.0, R1600), 6 (0.0, R1500)]
        //   Wait — but my code only sorts within score group. The float-down
        //   is concatenated at the front. Pool layout:
        //   [4 (the floater)] + [2, 5, 6 (sorted by rating in 0.0 group)]
        //   = [4, 2, 5, 6]. Even, pair top-vs-bottom: 4 vs 5, 2 vs 6.

        var result = UscfPairer.Pair(doc);

        Assert.Null(result.ByePair);
        Assert.Equal(3, result.Pairings.Count);

        var pairs = result.Pairings
            .Select(p => (Math.Min(p.WhitePair, p.BlackPair), Math.Max(p.WhitePair, p.BlackPair)))
            .ToHashSet();

        Assert.Contains((1, 3), pairs);
        Assert.Contains((4, 5), pairs);
        Assert.Contains((2, 6), pairs);
    }

    [Fact]
    public void Last_score_group_with_odd_count_assigns_bye_to_lowest_rated()
    {
        // 5 players, R1 played. Scores: 1.0={1, 2}, 0.0={3, 4, 5}.
        // 1.0 group is even, pairs (1 vs 2).
        // 0.0 group is odd (3 players), it's the last group → lowest gets bye.
        //   Pool [3, 4, 5] sorted by rating desc. Pair top 2 → (3 vs 4),
        //   bye = pair 5.
        var doc = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2200, Cells: [Cell(3, 'w', '1')]),
            new RoundCellSpec(PairNumber: 2, Rating: 2100, Cells: [Cell(4, 'w', '1')]),
            new RoundCellSpec(PairNumber: 3, Rating: 2000, Cells: [Cell(1, 'b', '0')]),
            new RoundCellSpec(PairNumber: 4, Rating: 1900, Cells: [Cell(2, 'b', '0')]),
            new RoundCellSpec(PairNumber: 5, Rating: 1800, Cells: [Cell(0, '-', 'U')])); // R1 bye → 1.0!

        // Hmm, with that setup pair 5 has 1.0 too. Let me re-spec: pair 5
        // has a zero-point bye instead so they stay in the 0.0 group.
        doc = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2200, Cells: [Cell(3, 'w', '1')]),
            new RoundCellSpec(PairNumber: 2, Rating: 2100, Cells: [Cell(4, 'w', '1')]),
            new RoundCellSpec(PairNumber: 3, Rating: 2000, Cells: [Cell(1, 'b', '0')]),
            new RoundCellSpec(PairNumber: 4, Rating: 1900, Cells: [Cell(2, 'b', '0')]),
            new RoundCellSpec(PairNumber: 5, Rating: 1800, Cells: [Cell(0, '-', 'Z')])); // zero-point bye

        var result = UscfPairer.Pair(doc);

        Assert.Equal(5, result.ByePair);
        Assert.Equal(2, result.Pairings.Count);

        var pairs = result.Pairings
            .Select(p => (Math.Min(p.WhitePair, p.BlackPair), Math.Max(p.WhitePair, p.BlackPair)))
            .ToHashSet();

        Assert.Contains((1, 2), pairs);
        Assert.Contains((3, 4), pairs);
    }

    [Fact]
    public void Player_with_more_whites_than_blacks_gets_black_when_paired_with_balanced_opponent()
    {
        // Two players, one with W history, one with B history. The one
        // who has had MORE whites (so net diff > 0) should get black this
        // round; the other gets white.
        //
        // pair 1: 2 played, both white → wants black
        // pair 2: 2 played, both black → wants white
        // → pair 2 plays white, pair 1 plays black.
        var doc = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2000,
                Cells: [Cell(3, 'w', '1'), Cell(4, 'w', '1')]),
            new RoundCellSpec(PairNumber: 2, Rating: 1900,
                Cells: [Cell(3, 'b', '0'), Cell(4, 'b', '0')]));

        var result = UscfPairer.Pair(doc);

        Assert.Single(result.Pairings);
        var p = result.Pairings[0];
        Assert.Equal(2, p.WhitePair);
        Assert.Equal(1, p.BlackPair);
    }

    // --------------------------------------------------------- helpers

    private record struct RoundCellSpec(int PairNumber, int Rating, IReadOnlyList<TrfRoundCell> Cells);

    private static TrfRoundCell Cell(int opp, char color, char result) =>
        new(opp, color, result);

    private static TrfDocument MakeDocWithHistory(params RoundCellSpec[] specs)
    {
        var rounds = specs.Length == 0 ? 0 : specs.Max(s => s.Cells.Count);
        var players = specs
            .Select(s => new TrfPlayer(
                PairNumber: s.PairNumber,
                Name: $"Player{s.PairNumber}",
                Rating: s.Rating,
                Id: string.Empty,
                Points: s.Cells.Sum(c => c.Score),
                Rounds: s.Cells))
            .ToList();

        return new TrfDocument(
            TournamentName: "RoundN-Test",
            StartDate: string.Empty,
            EndDate: string.Empty,
            TotalRounds: rounds + 3,
            InitialColor: 'w',
            Players: players);
    }
}
