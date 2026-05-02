using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Uscf.Trf;

namespace FreePair.Core.Uscf;

/// <summary>
/// FreePair's USCF Swiss pairing engine. Implements rounds 1 and 2+
/// at the matching-correctness level.
/// </summary>
/// <remarks>
/// <para><b>Round 1 algorithm (USCF 28C):</b></para>
/// <list type="number">
///   <item>Sort players by rating descending; break ties by pair number
///         ascending.</item>
///   <item>If the count is odd, the lowest-rated player gets a full-point
///         bye.</item>
///   <item>Split in half. Pair seed[i] with seed[i + half] — top half
///         plays bottom half.</item>
///   <item>Allocate colours: top seed of board 1 receives the
///         <see cref="TrfDocument.InitialColor"/> (XXC); subsequent
///         boards alternate.</item>
/// </list>
///
/// <para><b>Round 2+ algorithm (USCF 28D / 28L) — current scope:</b></para>
/// <list type="number">
///   <item>Compute each player's cumulative score from their TRF round
///         cells (1.0 win / 0.5 draw / 1.0 full-point bye / 0.5 half-bye).</item>
///   <item>Group players by score (highest score group first).</item>
///   <item>Within each group, sort by rating descending, ties by pair
///         number ascending.</item>
///   <item>If a group has an odd number of players (after any drop-down
///         from a higher group), float the lowest-rated player <em>down</em>
///         to the next score group. The floated player will be at the
///         <em>top</em> of the next group's pool, since their score is
///         higher.</item>
///   <item>Within each balanced score-group pool, pair top half against
///         bottom half (seed[i] vs seed[i + half]) — same matching shape
///         as round 1.</item>
///   <item>If the lowest-score group ends up with one unpairable player,
///         that player gets the round's full-point bye.</item>
///   <item>Colour allocation (placeholder for P3): give white to whichever
///         player has had fewer whites so far, breaking ties by giving the
///         higher seed the opposite of their last colour.</item>
/// </list>
///
/// <para><b>Not yet modelled (P2 / P3 / P4):</b></para>
/// <list type="bullet">
///   <item>Repeat-pairing avoidance via transpositions (USCF 28L1–L3) —
///         today the engine will happily pair two players who already
///         met if the simple top-half-vs-bottom-half rule schedules them.
///         The harness surfaces this as a hard mismatch when SwissSys
///         transposed to avoid it.</item>
///   <item>Full USCF 29D colour preference resolution
///         (absolute / strong / mild) — P1 uses a simplified
///         fewer-whites-gets-white tiebreaker.</item>
///   <item>Pre-flagged half-point byes / withdrawals embedded in the TRF
///         (those players must be filtered out by the caller before
///         <see cref="Pair"/> sees them).</item>
/// </list>
/// </remarks>
public static class UscfPairer
{
    /// <summary>
    /// Pairs the next round of <paramref name="document"/>. Routes to
    /// the round-1 algorithm when no player has any played history,
    /// otherwise routes to the round-N score-group pairer.
    /// </summary>
    public static UscfPairingResult Pair(TrfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Determine how many rounds of history we have. A "real" round
        // cell is one where the player either has an opponent or a bye
        // result.
        var maxPlayedRounds = document.Players.Count == 0
            ? 0
            : document.Players.Max(CountPlayedRounds);

        return maxPlayedRounds > 0
            ? PairRoundN(document)
            : PairRoundOne(document);
    }

    private static int CountPlayedRounds(TrfPlayer p)
    {
        var n = 0;
        foreach (var cell in p.Rounds)
        {
            if (cell.Opponent > 0 || cell.IsBye)
            {
                n++;
            }
        }
        return n;
    }

    private static UscfPairingResult PairRoundOne(TrfDocument document)
    {
        // USCF 28C: order players by rating (descending), break ties by
        // starting rank to keep the result deterministic for a given TRF.
        var ordered = document.Players
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.PairNumber)
            .ToArray();

        if (ordered.Length == 0)
        {
            return new UscfPairingResult(Array.Empty<UscfPairing>(), null);
        }

        // Odd count: the lowest-rated player gets a full-point bye
        // (USCF 28L: bye assignment in round 1 goes to the lowest-rated
        // player who has not requested no-bye; we don't yet model bye
        // requests, so this is "lowest seed").
        int? byePair = null;
        if (ordered.Length % 2 == 1)
        {
            byePair = ordered[^1].PairNumber;
            ordered = ordered[..^1];
        }

        var half = ordered.Length / 2;
        var initialIsWhite = (document.InitialColor ?? 'w') == 'w';

        var pairings = new List<UscfPairing>(half);
        for (var i = 0; i < half; i++)
        {
            var topSeed = ordered[i];
            var bottomSeed = ordered[i + half];

            // USCF 29E1: in round 1, half of the top seeds receive white
            // and half receive black, alternating from board 1's assigned
            // colour. So board i (0-indexed): top seed gets the initial
            // colour when i is even, the opposite when i is odd.
            var topGetsWhite = initialIsWhite ^ ((i & 1) == 1);

            var (white, black) = topGetsWhite
                ? (topSeed, bottomSeed)
                : (bottomSeed, topSeed);

            pairings.Add(new UscfPairing(white.PairNumber, black.PairNumber, Board: i + 1));
        }

        return new UscfPairingResult(pairings, byePair);
    }

    // ============================================================ Round N

    private static UscfPairingResult PairRoundN(TrfDocument document)
    {
        if (document.Players.Count == 0)
        {
            return new UscfPairingResult(Array.Empty<UscfPairing>(), null);
        }

        // Group active players by score. We assume the caller has already
        // filtered out anyone who shouldn't be paired this round (withdrawn,
        // pre-flagged half-byes, etc.). Score groups go highest first.
        var scoreGroups = document.Players
            .GroupBy(ComputeScore)
            .OrderByDescending(g => g.Key)
            .Select(g => g
                .OrderByDescending(p => p.Rating)
                .ThenBy(p => p.PairNumber)
                .ToList())
            .ToList();

        var pairings = new List<UscfPairing>();
        int? byePair = null;
        var board = 1;

        // Players who couldn't be paired in their natural score group and
        // are floating down into the next one. Always at the top of the
        // next pool because their score is higher than that group's.
        var floatDown = new List<TrfPlayer>();

        for (var gi = 0; gi < scoreGroups.Count; gi++)
        {
            var pool = floatDown.Concat(scoreGroups[gi]).ToList();
            floatDown = new List<TrfPlayer>();

            // Odd group → drop the lowest-rated player to the next group.
            // (Last score group's leftover becomes the round's bye, handled
            // after the loop.)
            if (pool.Count % 2 == 1 && gi < scoreGroups.Count - 1)
            {
                floatDown.Add(pool[^1]);
                pool.RemoveAt(pool.Count - 1);
            }

            board = PairPool(pool, board, pairings);

            // If we're on the last group and it's still odd, the leftover
            // is the bye.
            if (gi == scoreGroups.Count - 1 && pool.Count % 2 == 1)
            {
                // PairPool ignored the trailing odd one — now collect it.
                byePair = pool[^1].PairNumber;
            }
        }

        // Edge case: float-down accumulated past the last group (shouldn't
        // happen with the gi-aware odd guard above, but defensive).
        if (floatDown.Count > 0 && byePair is null)
        {
            byePair = floatDown[^1].PairNumber;
            floatDown.RemoveAt(floatDown.Count - 1);
            board = PairPool(floatDown, board, pairings);
        }

        return new UscfPairingResult(pairings, byePair);
    }

    /// <summary>
    /// Pairs an even-sized score-group pool top-half-vs-bottom-half,
    /// appending the resulting <see cref="UscfPairing"/>s to <paramref name="pairings"/>
    /// and returning the next board number to use. If the pool is odd,
    /// the trailing element is left unpaired (caller's responsibility to
    /// either float it down or assign as bye).
    /// </summary>
    private static int PairPool(IList<TrfPlayer> pool, int startBoard, List<UscfPairing> pairings)
    {
        var pairableCount = pool.Count - (pool.Count % 2);
        var half = pairableCount / 2;
        var board = startBoard;

        for (var i = 0; i < half; i++)
        {
            var top = pool[i];
            var bottom = pool[i + half];
            var topGetsWhite = TopGetsWhite(top, bottom);
            var (white, black) = topGetsWhite ? (top, bottom) : (bottom, top);
            pairings.Add(new UscfPairing(white.PairNumber, black.PairNumber, Board: board));
            board++;
        }

        return board;
    }

    private static decimal ComputeScore(TrfPlayer p)
    {
        decimal score = 0m;
        foreach (var cell in p.Rounds)
        {
            score += cell.Score;
        }
        return score;
    }

    /// <summary>
    /// Placeholder colour allocation for P1: whoever has played fewer
    /// whites so far gets white. Ties broken by giving the higher seed
    /// the colour opposite to their last actual colour. P3 will replace
    /// this with the full USCF 29D preference resolution (absolute /
    /// strong / mild).
    /// </summary>
    private static bool TopGetsWhite(TrfPlayer top, TrfPlayer bottom)
    {
        var topWhites = CountColor(top, 'w');
        var topBlacks = CountColor(top, 'b');
        var bottomWhites = CountColor(bottom, 'w');
        var bottomBlacks = CountColor(bottom, 'b');

        // Difference (whites - blacks). The player with the more-negative
        // diff (more blacks than whites) wants white.
        var topDiff = topWhites - topBlacks;
        var bottomDiff = bottomWhites - bottomBlacks;

        if (topDiff < bottomDiff) return true;   // top has fewer whites → wants white
        if (topDiff > bottomDiff) return false;  // bottom has fewer whites → top gets black

        // Equal preference: give the higher-rated seed the colour opposite
        // their last actual colour, falling back to "top gets white".
        var topLast = LastColor(top);
        return topLast switch
        {
            'w' => false,  // had white last → black this round
            'b' => true,   // had black last → white this round
            _   => true,   // no prior colour (only byes?) → top gets white
        };
    }

    private static int CountColor(TrfPlayer p, char color)
    {
        var n = 0;
        foreach (var cell in p.Rounds)
        {
            if (cell.Color == color) n++;
        }
        return n;
    }

    private static char LastColor(TrfPlayer p)
    {
        for (var i = p.Rounds.Count - 1; i >= 0; i--)
        {
            var c = p.Rounds[i].Color;
            if (c is 'w' or 'b') return c;
        }
        return '-';
    }
}
