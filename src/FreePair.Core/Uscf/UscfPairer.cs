using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Uscf.Trf;

namespace FreePair.Core.Uscf;

/// <summary>
/// FreePair's USCF Swiss pairing engine. Implements rounds 1 and 2+
/// at the matching-correctness level, with USCF 28L1-style transposition-
/// based repeat-pairing avoidance.
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
///   <item>USCF 28L1: when a natural pairing would force a rematch,
///         attempt a single transposition within the bottom half — swap
///         bot[i] with the closest bot[j] (j &gt; i) such that neither
///         resulting pair is a rematch. Prefer the smallest j (least
///         disturbance from natural rating order).</item>
///   <item>If the lowest-score group ends up with one unpairable player,
///         that player gets the round's full-point bye.</item>
///   <item>Colour allocation (placeholder for P3): give white to whichever
///         player has had fewer whites so far, breaking ties by giving the
///         higher seed the opposite of their last colour.</item>
/// </list>
///
/// <para><b>Not yet modelled (P2-future / P3 / P4):</b></para>
/// <list type="bullet">
///   <item>USCF 28L2-L3 deeper transpositions (multiple swaps,
///         interchanges across half-boundaries) and the colour-balance
///         constraints that govern which transposition is preferred when
///         several would resolve a rematch.</item>
///   <item>Full USCF 29D colour preference resolution
///         (absolute / strong / mild) — current code uses a simplified
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

        // Pre-flagged byes (P4): pull the players the TD has marked
        // for half-point / zero-point byes for the upcoming round out
        // of the pool BEFORE pairing. They flow through to the result
        // as RequestedByes; the rest of the pipeline never sees them.
        var requestedByes = ExtractRequestedByes(document, out var paired);

        // Determine how many rounds of history we have. A "real" round
        // cell is one where the player either has an opponent or a bye
        // result.
        var maxPlayedRounds = paired.Count == 0
            ? 0
            : paired.Max(CountPlayedRounds);

        // Build a filtered TrfDocument whose Players collection excludes
        // the pre-flagged bye recipients. Everything else (initial colour,
        // total rounds, etc.) carries through unchanged.
        var filteredDoc = paired.Count == document.Players.Count
            ? document
            : document with { Players = paired };

        var inner = maxPlayedRounds > 0
            ? PairRoundN(filteredDoc)
            : PairRoundOne(filteredDoc);

        // Splice the pre-flagged byes back into the result. The
        // auto-assigned full-point bye (ByePair) remains separate
        // because USCF treats it as a different concept (forced bye
        // due to odd field, not a TD-requested skip).
        return requestedByes.Count == 0
            ? inner
            : inner with { RequestedByes = requestedByes };
    }

    /// <summary>
    /// Splits <paramref name="document"/>'s player list into:
    ///   - the pairable subset (returned via <paramref name="pairable"/>),
    ///   - the pre-flagged byes (returned as the function result, in
    ///     pair-number order).
    /// When the document has no <see cref="TrfDocument.RequestedByes"/>
    /// dictionary, returns an empty bye list and the original players
    /// list verbatim.
    /// </summary>
    private static IReadOnlyList<UscfRequestedBye> ExtractRequestedByes(
        TrfDocument document, out IReadOnlyList<TrfPlayer> pairable)
    {
        if (document.RequestedByes is null || document.RequestedByes.Count == 0)
        {
            pairable = document.Players;
            return Array.Empty<UscfRequestedBye>();
        }

        var byes = new List<UscfRequestedBye>();
        var keep = new List<TrfPlayer>(document.Players.Count);
        foreach (var p in document.Players)
        {
            if (document.RequestedByes.TryGetValue(p.PairNumber, out var kind)
                && (kind == 'H' || kind == 'Z'))
            {
                byes.Add(new UscfRequestedBye(p.PairNumber, kind));
            }
            else
            {
                keep.Add(p);
            }
        }

        byes.Sort((a, b) => a.PairNumber.CompareTo(b.PairNumber));
        pairable = keep;
        return byes;
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

        var initialColor = document.InitialColor ?? 'w';

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
        // are floating down into the next one. Per USCF 28F2, a downfloater
        // plays the HIGHEST-rated player of the lower group (not the
        // lowest-rated of the top-half-vs-bottom-half slide). To honour
        // this with the same SLIDE machinery the rest of the pairer
        // uses, we insert floaters at the TOP of the BOTTOM HALF of
        // the combined pool — index = halfCount — so PairPool's slide
        // pairs floater[0] with topHalf[0] (= highest-rated of the
        // group). This matches SwissSys 11's behaviour and is what
        // most live-event TDs expect from a USCF Swiss pairer.
        var floatDown = new List<TrfPlayer>();

        for (var gi = 0; gi < scoreGroups.Count; gi++)
        {
            var groupSorted = scoreGroups[gi];
            var pool = MergeWithFloaters(groupSorted, floatDown);
            floatDown = new List<TrfPlayer>();

            // Odd group → drop the lowest-rated player to the next group.
            // (Last score group's leftover becomes the round's bye, handled
            // after the loop.) The floater(s) we just embedded are
            // intentionally NOT eligible to drop — pool[^1] is always
            // the lowest of the original score group, never a floater,
            // because floaters live at indices halfCount .. halfCount+F-1.
            if (pool.Count % 2 == 1 && gi < scoreGroups.Count - 1)
            {
                floatDown.Add(pool[^1]);
                pool.RemoveAt(pool.Count - 1);
            }

            board = PairPool(pool, board, pairings, initialColor);

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
            board = PairPool(floatDown, board, pairings, initialColor);
        }

        return new UscfPairingResult(pairings, byePair);
    }

    /// <summary>
    /// Builds the combined pool used by <see cref="PairPool"/> for a
    /// score group, embedding any downfloaters from higher groups at
    /// the TOP of the BOTTOM HALF rather than at index 0 of the pool.
    /// </summary>
    /// <remarks>
    /// <para>USCF 28F2 says a downfloater plays the highest-rated
    /// player of the lower score group. Encoding that into the SLIDE
    /// pairing machinery requires positioning the floater so that
    /// <c>top[0]</c> (highest-rated of the lower group) ends up
    /// paired with <c>bot[0]</c> (= the floater). Concretely: with
    /// G original group players + F floaters and total halfCount
    /// = (G+F)/2, the layout is:</para>
    /// <code>
    ///   pool[0..halfCount]                = first halfCount of G
    ///   pool[halfCount..halfCount+F]      = floaters (kept in arrival order)
    ///   pool[halfCount+F..G+F]            = remaining G - halfCount of G
    /// </code>
    /// <para>This guarantees pool[^1] is always the lowest-rated of
    /// the original group (never a floater) so the odd-group "drop
    /// the last one to the next group" logic continues to drop the
    /// right player.</para>
    /// </remarks>
    private static List<TrfPlayer> MergeWithFloaters(
        IReadOnlyList<TrfPlayer> groupSorted,
        IReadOnlyList<TrfPlayer> floaters)
    {
        if (floaters.Count == 0)
        {
            return new List<TrfPlayer>(groupSorted);
        }

        var total = groupSorted.Count + floaters.Count;
        var halfCount = total / 2;

        // Defensive: when the group is so small that all of it is in
        // the top half (halfCount > groupSorted.Count), fall back to
        // the simple prepend layout. Practically this only happens in
        // pathological cases (e.g. a group of 1 receiving a floater)
        // where SLIDE doesn't really apply anyway.
        if (halfCount >= groupSorted.Count)
        {
            return floaters.Concat(groupSorted).ToList();
        }

        var pool = new List<TrfPlayer>(total);
        for (var i = 0; i < halfCount; i++) pool.Add(groupSorted[i]);
        foreach (var f in floaters) pool.Add(f);
        for (var i = halfCount; i < groupSorted.Count; i++) pool.Add(groupSorted[i]);
        return pool;
    }


    /// <summary>
    /// Pairs an even-sized score-group pool top-half-vs-bottom-half,
    /// applying USCF 28L1-style transpositions on the bottom half when
    /// the natural pairing would force a rematch. Appends the resulting
    /// <see cref="UscfPairing"/>s to <paramref name="pairings"/> and
    /// returns the next board number to use. If the pool is odd, the
    /// trailing element is left unpaired (caller's responsibility to
    /// either float it down or assign as bye).
    /// </summary>
    private static int PairPool(IList<TrfPlayer> pool, int startBoard, List<UscfPairing> pairings, char initialColor)
    {
        var pairableCount = pool.Count - (pool.Count % 2);
        var half = pairableCount / 2;
        var board = startBoard;

        // Take the top and bottom halves. Then run a backtracking
        // matcher (USCF 28L1-L2 deeper transpositions) that searches
        // for an assignment of bot[*] to top[*] avoiding rematches,
        // preferring the natural rating order whenever feasible. The
        // search visits bot[i]'s natural counterpart first, then the
        // closest non-natural choices in widening rings, so when no
        // rematches exist we get the same output the simple zip would
        // produce; when one or two rematches exist we transpose only
        // as much as needed; and when many rematches exist we still
        // find a non-rematch matching as long as one is mathematically
        // possible (single-swap couldn't, full backtracking can).
        //
        // SLIDE vs FOLD: we use SLIDE — top[0] plays bot[0], top[1]
        // plays bot[1], etc. (USCF 28D2(d), explicit). SwissSys
        // matches us on most score groups but occasionally uses fold-
        // shaped output; when it does, it's because of a downstream
        // optimisation (colour balance at the GROUP level, downfloat
        // reassignment, etc.) we haven't identified yet — see
        // UscfMccDiagnosticTests for concrete divergence cases.
        var top = pool.Take(half).ToList();
        var bot = pool.Skip(half).Take(half).ToList();
        var assignment = new TrfPlayer[half];

        if (!TryFindNonRematchMatching(top, bot, assignment))
        {
            // 28L3 cross-half interchange (P2-deeper-still): when no
            // within-half non-rematch matching exists, USCF allows
            // SWAPPING one player from the top half with one from
            // the bottom half, then re-running the standard top-vs-
            // bot pairing on the new halves. The smallest disturbance
            // is interchanging the lowest-rated top with the highest-
            // rated bot (i = half-1, j = 0); we widen from there.
            //
            // Concrete case: GBO 2025 Premier R4, score 1.5 group of 4
            // players (#6, #10, #15, #18). Both SLIDE (6-15, 10-18)
            // and FOLD (6-18, 10-15) have one rematch each. Only the
            // cross-half (6-10, 15-18) is rematch-free, and SwissSys
            // produced exactly that.
            if (!TryCrossHalfInterchange(top, bot, assignment, out var newTop, out var newBot))
            {
                // No 28L3 solution either — commit the natural
                // pairing as a least-bad fallback.
                for (var i = 0; i < half; i++) assignment[i] = bot[i];
            }
            else
            {
                top = (List<TrfPlayer>)newTop;
                bot = (List<TrfPlayer>)newBot;
            }
        }

        for (var i = 0; i < half; i++)
        {
            var topPlayer = top[i];
            var bottomPlayer = assignment[i];
            var topGetsWhite = TopGetsWhite(topPlayer, bottomPlayer, initialColor, board);
            var (white, black) = topGetsWhite ? (topPlayer, bottomPlayer) : (bottomPlayer, topPlayer);
            pairings.Add(new UscfPairing(white.PairNumber, black.PairNumber, Board: board));
            board++;
        }

        return board;
    }

    /// <summary>
    /// USCF 28L3 cross-half interchange. When the within-half
    /// matcher (<see cref="TryFindNonRematchMatching"/>) returns
    /// false because every bot[*] arrangement still produces a
    /// rematch, swap ONE player from the top half with ONE player
    /// from the bottom half and re-run the matcher on the new
    /// halves. Iterates interchange candidates in order of
    /// disturbance (smallest first — swap the lowest-rated top
    /// with the highest-rated bot, i.e. <c>i == half-1 &amp;&amp; j == 0</c>),
    /// returning the first interchange that yields a rematch-free
    /// assignment.
    /// </summary>
    /// <param name="newTop">
    /// New top half with the interchange applied, when the method
    /// returns <c>true</c>. Same reference as <paramref name="top"/>
    /// when it returns <c>false</c>.
    /// </param>
    /// <param name="newBot">Same convention for the bottom half.</param>
    /// <returns>
    /// <c>true</c> when a single interchange yielded a rematch-free
    /// matching; <c>false</c> when every interchange still produced
    /// at least one rematch. Multi-step interchanges (swap two from
    /// each half, etc.) aren't attempted — they're rare in practice
    /// and would warrant a deeper investigation if they ever showed
    /// up in the corpus.
    /// </returns>
    private static bool TryCrossHalfInterchange(
        IList<TrfPlayer> top, IList<TrfPlayer> bot,
        TrfPlayer[] assignment,
        out IList<TrfPlayer> newTop, out IList<TrfPlayer> newBot)
    {
        var n = top.Count;

        // Disturbance metric: distance from the boundary swap
        // (i == n-1, j == 0). Small disturbance = swap players
        // closest to the rating boundary; large disturbance =
        // swap players far from the boundary.
        var candidates = new List<(int i, int j, int disturbance)>(n * n);
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                candidates.Add((i, j, (n - 1 - i) + j));
            }
        }
        candidates.Sort((a, b) => a.disturbance.CompareTo(b.disturbance));

        foreach (var (i, j, _) in candidates)
        {
            // Skip the no-op swap (only happens when n == 1, but
            // covered defensively).
            if (top[i].PairNumber == bot[j].PairNumber) continue;

            // Apply the interchange to fresh lists so failed
            // candidates don't pollute the next attempt.
            var trialTop = top.ToList();
            var trialBot = bot.ToList();
            (trialTop[i], trialBot[j]) = (trialBot[j], trialTop[i]);

            if (TryFindNonRematchMatching(trialTop, trialBot, assignment))
            {
                newTop = trialTop;
                newBot = trialBot;
                return true;
            }
        }

        newTop = top;
        newBot = bot;
        return false;
    }

    /// <summary>
    /// Backtracking bipartite matcher for the score-group's top vs
    /// bottom halves. Tries to assign each <c>top[i]</c> a partner
    /// from <paramref name="bot"/> such that no pair is a rematch.
    /// Search order at each level prefers natural rating-order pairs
    /// (j == i) first, then walks outward in widening rings, so an
    /// arrangement with the fewest disturbances vs the natural pairing
    /// is found first.
    /// </summary>
    /// <returns>
    /// <c>true</c> on success, with <paramref name="assignment"/>
    /// filled in (parallel to <paramref name="top"/>); <c>false</c>
    /// when no rematch-free matching exists. Callers should fall
    /// back to the natural pairing on <c>false</c>.
    /// </returns>
    private static bool TryFindNonRematchMatching(
        IList<TrfPlayer> top, IList<TrfPlayer> bot, TrfPlayer[] assignment)
    {
        var n = top.Count;
        var used = new bool[n];
        return Recurse(0);

        bool Recurse(int i)
        {
            if (i >= n) return true;
            foreach (var j in NaturalOrder(i, n))
            {
                if (used[j]) continue;
                if (IsForbiddenPair(top[i], bot[j])) continue;
                used[j] = true;
                assignment[i] = bot[j];
                if (Recurse(i + 1)) return true;
                used[j] = false;
            }
            return false;
        }
    }

    /// <summary>
    /// Yields the indices <c>0..count-1</c> in the order
    /// <c>i, i+1, i+2, …, count-1, i-1, i-2, …, 0</c> — natural
    /// counterpart first, then forward neighbours (smallest-j-first
    /// per USCF 28L1's preference for the least-disturbing
    /// transposition), then backward neighbours as a last resort.
    /// </summary>
    private static IEnumerable<int> NaturalOrder(int i, int count)
    {
        if (i >= 0 && i < count) yield return i;
        for (var j = i + 1; j < count; j++) yield return j;
        for (var j = i - 1; j >= 0; j--) yield return j;
    }

    /// <summary>
    /// True when <paramref name="a"/>'s round history shows a game
    /// against <paramref name="b"/>. Byes / unpaired cells are ignored.
    /// </summary>
    private static bool HasPlayed(TrfPlayer a, TrfPlayer b)
    {
        foreach (var cell in a.Rounds)
        {
            if (cell.Opponent == b.PairNumber) return true;
        }
        return false;
    }

    /// <summary>
    /// True when both players carry a non-empty <see cref="TrfPlayer.Team"/>
    /// label and the labels match (case-insensitive). Used by the
    /// matcher to avoid pairing siblings / teammates — Puddletown's
    /// scholastic Swisses tag families this way and SwissSys honours
    /// the constraint at the matching layer (it's effectively a
    /// rematch from the player's point of view: "we're a household,
    /// don't pair us"). Blank teams short-circuit to <c>false</c> so
    /// non-team-tagged tournaments behave exactly as before.
    /// </summary>
    private static bool ShareTeam(TrfPlayer a, TrfPlayer b)
    {
        if (string.IsNullOrWhiteSpace(a.Team)) return false;
        if (string.IsNullOrWhiteSpace(b.Team)) return false;
        return string.Equals(a.Team, b.Team, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true when the candidate pair (<paramref name="top"/>,
    /// <paramref name="bot"/>) violates a <em>matching-time</em>
    /// constraint — currently rematch avoidance and same-team
    /// avoidance. The matcher rejects any candidate that returns true
    /// before recursing, so the search only ever produces
    /// constraint-clean assignments.
    /// </summary>
    private static bool IsForbiddenPair(TrfPlayer top, TrfPlayer bot) =>
        HasPlayed(top, bot) || ShareTeam(top, bot);

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
    /// USCF 29D colour allocation. Decides which of the two players in
    /// a pair receives white. The rule order matches USCF 29D1 → 29D2:
    /// equalise first, then alternate, with a streak-absolute exception
    /// at the top.
    /// </summary>
    /// <remarks>
    /// <para>Priority (top-to-bottom, first decisive answer wins):</para>
    /// <list type="number">
    ///   <item><b>Streak absolute (29D5).</b> A player who has played two
    ///         actual games of the same colour in a row has an absolute
    ///         claim on the opposite colour. If exactly one of the
    ///         pair has such a streak, they are given the opposite
    ///         colour; if both do (one each direction), satisfy both;
    ///         if both want the same colour we fall through to
    ///         equalisation.</item>
    ///   <item><b>Equalisation (29D1).</b> Player with more whites
    ///         (higher whites − blacks) gets black; the other gets
    ///         white. Strict inequality only — ties fall through.</item>
    ///   <item><b>Alternation (29D2).</b> Compare last actual-game
    ///         colours. The player whose last game was white gets
    ///         black this round; vice versa. Asymmetric histories
    ///         (one has a colour on record, the other doesn't) follow
    ///         the same rule against the implicit "no preference".</item>
    ///   <item><b>Round-1 fallback.</b> Both players have identical
    ///         (typically empty) histories. Pair behaves like a R1 pair
    ///         on the given <paramref name="board"/>: the top seed gets
    ///         the round-1 initial colour on odd boards, the opposite
    ///         on even boards.</item>
    /// </list>
    /// </remarks>
    private static bool TopGetsWhite(TrfPlayer top, TrfPlayer bottom, char initialColor, int board)
    {
        // (1) Streak absolute: two same-colour games in a row → must
        //     get the opposite colour. When both players have streaks
        //     of opposite colours we satisfy both; same-direction
        //     streaks fall through to equalisation as a tiebreaker.
        var topStreak = StreakColor(top);   // colour the top has TOO MUCH OF
        var botStreak = StreakColor(bottom);
        if (topStreak == 'w' && botStreak != 'w') return false; // top must get black
        if (topStreak == 'b' && botStreak != 'b') return true;  // top must get white
        if (botStreak == 'w' && topStreak != 'w') return true;  // bottom must get black → top white
        if (botStreak == 'b' && topStreak != 'b') return false; // bottom must get white → top black

        // (2) Equalise: whoever has played more whites gets black.
        var topDiff = CountColor(top, 'w') - CountColor(top, 'b');
        var botDiff = CountColor(bottom, 'w') - CountColor(bottom, 'b');
        if (topDiff < botDiff) return true;   // top has fewer whites
        if (topDiff > botDiff) return false;  // top has more whites

        // (3) Alternate: last actual-game colour decides.
        var topLast = LastColor(top);
        return topLast switch
        {
            'w' => false,  // had white last → black this round
            'b' => true,   // had black last → white this round
            _   => InitialColorOnBoard(initialColor, board), // (4) R1 fallback
        };
    }

    /// <summary>
    /// Returns the colour the top seed gets on board <paramref name="board"/>
    /// under the round-1 alternation pattern: top seed of board 1 gets
    /// <paramref name="initialColor"/>; subsequent boards alternate. Used
    /// as the final tiebreaker in <see cref="TopGetsWhite"/>.
    /// </summary>
    private static bool InitialColorOnBoard(char initialColor, int board)
    {
        var initialIsWhite = initialColor == 'w';
        // board is 1-based; board 1 → top gets initial colour (no flip).
        return initialIsWhite ^ (((board - 1) & 1) == 1);
    }

    /// <summary>
    /// If the player's last two actual-game colours are the same,
    /// returns that colour (the one they have "too much of" — they
    /// must get the OPPOSITE next round). Otherwise returns '-'.
    /// </summary>
    private static char StreakColor(TrfPlayer p)
    {
        var last       = '-';
        var secondLast = '-';
        for (var i = p.Rounds.Count - 1; i >= 0; i--)
        {
            var c = p.Rounds[i].Color;
            if (c is not ('w' or 'b')) continue;
            if (last == '-') { last = c; continue; }
            secondLast = c;
            break;
        }
        return last != '-' && last == secondLast ? last : '-';
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
