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
        // 6 players. After R1: pairs 1, 3, 4 have 1.0 (winners + a bye);
        // pairs 2, 5, 6 have 0.0 (losers + an unpaired). Both groups are
        // odd (3+3), so the lowest-rated of the 1.0 group (pair 4) floats
        // down to the 0.0 group.
        //
        // Walk-through with USCF 28F2 floater placement (floater is
        // inserted at the TOP of the BOTTOM HALF of the combined pool
        // so SLIDE pairs them with the highest-rated of the lower
        // group — see UscfPairer.MergeWithFloaters):
        //
        //   1.0 group: [1 (R2200), 3 (R2000), 4 (R1700)]. Odd, drop pair
        //              4 to floatDown. Remaining [1, 3] → pair (1 vs 3).
        //   0.0 group: groupSorted = [2 (R2100), 5 (R1600), 6 (R1500)].
        //              floater = [4]. halfCount = 4/2 = 2.
        //              Pool layout = top half [2, 5] + floater [4]
        //              + bot half [6] = [2, 5, 4, 6]. SLIDE half=2:
        //              top = [2, 5], bot = [4, 6] → (2 vs 4), (5 vs 6).
        //              Per 28F2, floater 4 is paired with the highest
        //              of the next group (pair 2). Neither pair is a
        //              rematch (4 played 5 in R1, not 2; 5 played 4
        //              in R1, not 6).
        //   No bye.
        var doc = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2200, Cells: [Cell(2, 'w', '1')]),
            new RoundCellSpec(PairNumber: 2, Rating: 2100, Cells: [Cell(1, 'b', '0')]),
            new RoundCellSpec(PairNumber: 3, Rating: 2000, Cells: [Cell(0, '-', 'U')]),  // R1 bye, score 1.0
            new RoundCellSpec(PairNumber: 4, Rating: 1700, Cells: [Cell(5, 'w', '1')]),
            new RoundCellSpec(PairNumber: 5, Rating: 1600, Cells: [Cell(4, 'b', '0')]),
            new RoundCellSpec(PairNumber: 6, Rating: 1500, Cells: [Cell(0, '-', '-')])); // R1 unpaired

        var result = UscfPairer.Pair(doc);

        Assert.Null(result.ByePair);
        Assert.Equal(3, result.Pairings.Count);

        var pairs = result.Pairings
            .Select(p => (Math.Min(p.WhitePair, p.BlackPair), Math.Max(p.WhitePair, p.BlackPair)))
            .ToHashSet();

        Assert.Contains((1, 3), pairs);   // 1.0 group
        Assert.Contains((2, 4), pairs);   // floater (4) with highest of 0.0 (2) — USCF 28F2
        Assert.Contains((5, 6), pairs);   // remaining 0.0 players
        Assert.DoesNotContain((4, 5), pairs);  // would have been a rematch
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

    [Fact]
    public void Single_swap_transposition_avoids_a_rematch_in_score_group()
    {
        // 4 players in the same 1.0 score group, all won R1. The natural
        // top-half-vs-bottom-half pairing for R2 would put pair 1 vs
        // pair 3, but they already played in R1 — so we transpose pair 3
        // with pair 4 in the bottom half:
        //   natural:  (1 vs 3), (2 vs 4)  ← (1, 3) is a rematch
        //   after L1: (1 vs 4), (2 vs 3)
        //
        // History setup (all 1.0 score):
        //   pair 1: R1 win vs pair 3 (W)
        //   pair 2: R1 win vs pair 4 (W)
        //   pair 3: R1 loss vs pair 1 (B)  -- still in 1.0 group? No, 0.0.
        //
        // Re-spec so all four end up in the same score group: pair 1+2
        // each won by full-point bye, pair 3+4 each won by full-point bye.
        // That puts all four in the 1.0 group with no prior interactions —
        // not what we want.
        //
        // Re-spec with mixed history that still produces a same-score
        // group: pair 1 won vs pair 3, pair 2 won vs pair 4 (R1) — that
        // gives pair 1+2 score 1.0 and pair 3+4 score 0.0. To force pair 1
        // to face pair 3 again, we need them in the same score group.
        //
        // Cleanest construction: 4 players, R1 was bye-everyone (so all
        // have 1.0 from full-point bye), but stamp pair 1's history with
        // an explicit "played pair 3" record. That's contrived but tests
        // the swap logic.
        var doc = MakeDocWithHistory(
            // pair 1 has the meta history: played pair 3 in R0, R1 was a bye
            new RoundCellSpec(PairNumber: 1, Rating: 2000,
                Cells: [Cell(3, 'w', '1'), Cell(0, '-', 'U')]),
            new RoundCellSpec(PairNumber: 2, Rating: 1900,
                Cells: [Cell(4, 'w', '1'), Cell(0, '-', 'U')]),
            new RoundCellSpec(PairNumber: 3, Rating: 1800,
                Cells: [Cell(1, 'b', '0'), Cell(0, '-', 'U')]),
            new RoundCellSpec(PairNumber: 4, Rating: 1700,
                Cells: [Cell(2, 'b', '0'), Cell(0, '-', 'U')]));

        // Scores after the constructed history:
        //   pair 1: 1 (win) + 1 (bye) = 2.0
        //   pair 2: 1 (win) + 1 (bye) = 2.0
        //   pair 3: 0 (loss) + 1 (bye) = 1.0
        //   pair 4: 0 (loss) + 1 (bye) = 1.0
        // 2.0 group: [1, 2] → pair (1 vs 2). They've never played.
        // 1.0 group: [3, 4] → pair (3 vs 4). They've never played.
        //
        // No transposition triggered here. Skip this case and use a
        // 4-player single-group construction instead:

        doc = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2000,
                Cells: [Cell(3, 'w', '1')]),                 // played 3
            new RoundCellSpec(PairNumber: 2, Rating: 1900,
                Cells: [Cell(4, 'w', '1')]),                 // played 4
            new RoundCellSpec(PairNumber: 3, Rating: 1800,
                Cells: [Cell(1, 'b', '0')]),                 // played 1
            new RoundCellSpec(PairNumber: 4, Rating: 1700,
                Cells: [Cell(2, 'b', '0')]));                // played 2
        // Scores: 1+2 have 1.0, 3+4 have 0.0.
        // 1.0 group natural pairing: (1 vs 2). They haven't played → no swap.
        // 0.0 group natural pairing: (3 vs 4). They haven't played → no swap.
        var result1 = UscfPairer.Pair(doc);
        Assert.Equal(2, result1.Pairings.Count);
        var pairs1 = result1.Pairings
            .Select(p => (Math.Min(p.WhitePair, p.BlackPair), Math.Max(p.WhitePair, p.BlackPair)))
            .ToHashSet();
        Assert.Contains((1, 2), pairs1);
        Assert.Contains((3, 4), pairs1);

        // Now construct a case where a transposition IS needed.
        // Round 1: 1 played 2 (W), 3 played 4 (W). All draws.
        // Round 2: all four players have score 0.5 (same score group).
        //   Natural top-half-vs-bottom-half: top half [1, 2] (rating
        //   sort), bottom half [3, 4]. Pair (1 vs 3), (2 vs 4) — wait,
        //   but the natural pairing within a group sorted by rating is
        //   seed[i] vs seed[i+half], so for [1, 2, 3, 4]: (1 vs 3),
        //   (2 vs 4). Neither is a rematch with R1's (1-2, 3-4) results,
        //   so no swap triggered.
        //
        // Better: round 1 was 1v3 and 2v4 (both draws → all 0.5). Then
        // round 2 natural is (1 vs 3) again — rematch! Swap to
        // (1 vs 4), (2 vs 3).
        var doc2 = MakeDocWithHistory(
            new RoundCellSpec(PairNumber: 1, Rating: 2000,
                Cells: [Cell(3, 'w', '=')]),  // drew with 3
            new RoundCellSpec(PairNumber: 2, Rating: 1900,
                Cells: [Cell(4, 'w', '=')]),  // drew with 4
            new RoundCellSpec(PairNumber: 3, Rating: 1800,
                Cells: [Cell(1, 'b', '=')]),  // drew with 1
            new RoundCellSpec(PairNumber: 4, Rating: 1700,
                Cells: [Cell(2, 'b', '=')])); // drew with 2

        // All four have 0.5 — single score group.
        // Natural R2 pairing within [1, 2, 3, 4]: (1 vs 3), (2 vs 4).
        //   (1 vs 3) is a rematch. Swap bot[0] (=3) with bot[1] (=4):
        //   the transposition gives (1 vs 4), (2 vs 3) — neither has
        //   played each other. Transposition succeeds.
        var result2 = UscfPairer.Pair(doc2);
        Assert.Equal(2, result2.Pairings.Count);
        var pairs2 = result2.Pairings
            .Select(p => (Math.Min(p.WhitePair, p.BlackPair), Math.Max(p.WhitePair, p.BlackPair)))
            .ToHashSet();

        Assert.Contains((1, 4), pairs2);  // transposed
        Assert.Contains((2, 3), pairs2);  // transposed
        Assert.DoesNotContain((1, 3), pairs2);  // would be a rematch
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
