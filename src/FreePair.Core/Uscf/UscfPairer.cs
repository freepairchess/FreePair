using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Tournaments;
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
        // USCF 28C/28E: order players by rating (descending), break ties
        // alphabetically by last name then first name (names are stored
        // as "Last, First"), then by pair number as a final deterministic
        // tiebreaker.
        var ordered = document.Players
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
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
        var annotations = new List<PairingAnnotation>();

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

            var board = i + 1;
            pairings.Add(new UscfPairing(white.PairNumber, black.PairNumber, Board: board));

            annotations.Add(new PairingAnnotation(board, PairingReason.RoundOneSlide,
                $"Natural R1 slide: #{topSeed.PairNumber} (top half, rated {topSeed.Rating}) vs #{bottomSeed.PairNumber} (bottom half, rated {bottomSeed.Rating})"));
            annotations.Add(new PairingAnnotation(board,
                topGetsWhite ? PairingReason.ColorByInitialRule : PairingReason.ColorByInitialRule,
                topGetsWhite
                    ? $"White: #{topSeed.PairNumber} (top seed gets initial color on board {board})"
                    : $"White: #{bottomSeed.PairNumber} (top seed gets opposite color on even board {board})"));
        }

        if (byePair is not null)
        {
            annotations.Add(new PairingAnnotation(0, PairingReason.ByeAssigned,
                $"Full-point bye: #{byePair.Value} (lowest-rated player in R1)"));
        }

        return new UscfPairingResult(pairings, byePair, Annotations: annotations);
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
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.PairNumber)
                .ToList())
            .ToList();

        var pairings = new List<UscfPairing>();
        var annotations = new List<PairingAnnotation>();
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
            var currentFloaterCount = floatDown.Count;
            var pool = MergeWithFloaters(groupSorted, floatDown);
            floatDown = new List<TrfPlayer>();

            // Odd group → drop a player to the next group. Prefer the
            // natural SLIDE drop (lowest-rated candidate). However, if the
            // natural SLIDE produces ≥2 color conflicts and an alternative
            // drop yields 0 conflicts, prefer the color-friendly drop
            // (matching SwissSys behavior). Floaters don't re-float.
            if (pool.Count % 2 == 1 && gi < scoreGroups.Count - 1)
            {
                var floaterCount = pool.Count - groupSorted.Count;
                var mergeHalf = pool.Count / 2;

                int naturalIdx = -1;
                int naturalConflicts = int.MaxValue;
                int bestColorIdx = -1;
                int bestColorConflicts = int.MaxValue;

                for (var di = pool.Count - 1; di >= 0; di--)
                {
                    // Skip floater indices (they sit at mergeHalf..mergeHalf+floaterCount-1).
                    if (di >= mergeHalf && di < mergeHalf + floaterCount)
                        continue;

                    var testPool = pool.Where((_, idx) => idx != di).ToList();
                    var testHalf = testPool.Count / 2;
                    var testTop = testPool.Take(testHalf).ToList();
                    var testBot = testPool.Skip(testHalf).Take(testHalf).ToList();
                    var testAssign = new TrfPlayer[testHalf];

                    if (!TryFindNonRematchMatching(testTop, testBot, testAssign))
                        continue;

                    // Count color conflicts for this matching.
                    var pairs = new List<(TrfPlayer A, TrfPlayer B)>(testHalf);
                    for (var k = 0; k < testHalf; k++)
                        pairs.Add((testTop[k], testAssign[k]));
                    var colorConflicts = CountColorConflictPairs(pairs);

                    // Check if the solution is natural SLIDE.
                    var isNatural = true;
                    for (var k = 0; k < testHalf; k++)
                    {
                        if (testAssign[k].PairNumber != testBot[k].PairNumber)
                        { isNatural = false; break; }
                    }

                    if (isNatural && naturalIdx == -1)
                    {
                        naturalIdx = di;
                        naturalConflicts = colorConflicts;
                        if (colorConflicts == 0) break; // perfect — stop searching
                    }

                    if (colorConflicts < bestColorConflicts)
                    {
                        bestColorIdx = di;
                        bestColorConflicts = colorConflicts;
                    }
                }

                // Use the natural SLIDE drop unless a color-friendly
                // alternative produces strictly fewer color conflicts.
                int dropIdx;
                if (naturalIdx >= 0 && naturalConflicts <= bestColorConflicts)
                    dropIdx = naturalIdx;
                else if (bestColorIdx >= 0 && bestColorConflicts < naturalConflicts)
                    dropIdx = bestColorIdx;
                else if (naturalIdx >= 0)
                    dropIdx = naturalIdx;
                else if (bestColorIdx >= 0)
                    dropIdx = bestColorIdx;
                else
                    dropIdx = pool.Count - 1;

                floatDown.Add(pool[dropIdx]);
                var droppedPlayer = pool[dropIdx];
                var dropReason = (dropIdx == naturalIdx)
                    ? PairingReason.FloaterDropNatural
                    : PairingReason.FloaterDropColorFriendly;
                annotations.Add(new PairingAnnotation(0, dropReason,
                    $"Floater drop: #{droppedPlayer.PairNumber} (rated {droppedPlayer.Rating}) dropped from score group {ComputeScore(droppedPlayer):F1} — {(dropReason == PairingReason.FloaterDropNatural ? "natural SLIDE drop" : "color-friendly alternative")}"));
                pool.RemoveAt(dropIdx);
            }

            board = PairPool(pool, board, pairings, initialColor, annotations, currentFloaterCount);

            // If we're on the last group and it's still odd, the leftover
            // is the bye.
            if (gi == scoreGroups.Count - 1 && pool.Count % 2 == 1)
            {
                // PairPool ignored the trailing odd one — now collect it.
                byePair = pool[^1].PairNumber;
                annotations.Add(new PairingAnnotation(0, PairingReason.ByeAssigned,
                    $"Full-point bye: #{byePair.Value} (lowest-rated in last score group)"));
            }
        }

        // Edge case: float-down accumulated past the last group (shouldn't
        // happen with the gi-aware odd guard above, but defensive).
        if (floatDown.Count > 0 && byePair is null)
        {
            byePair = floatDown[^1].PairNumber;
            floatDown.RemoveAt(floatDown.Count - 1);
            board = PairPool(floatDown, board, pairings, initialColor, annotations, 0);
        }

        return new UscfPairingResult(pairings, byePair, Annotations: annotations);
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
        // the simple prepend layout.
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
    private static int PairPool(IList<TrfPlayer> pool, int startBoard, List<UscfPairing> pairings, char initialColor, List<PairingAnnotation> annotations, int floaterCount = 0)
    {
        var pairableCount = pool.Count - (pool.Count % 2);
        var half = pairableCount / 2;
        var board = startBoard;

        var top = pool.Take(half).ToList();
        var bot = pool.Skip(half).Take(half).ToList();
        var assignment = new TrfPlayer[half];

        var matchingKind = PairingReason.NaturalSlide;

        if (!TryFindNonRematchMatching(top, bot, assignment))
        {
            if (!TryCrossHalfInterchange(top, bot, assignment, out var newTop, out var newBot))
            {
                // No 28L3 solution either — commit the natural
                // pairing as a least-bad fallback.
                for (var i = 0; i < half; i++) assignment[i] = bot[i];
                matchingKind = PairingReason.FallbackRematchAccepted;
            }
            else
            {
                top = (List<TrfPlayer>)newTop;
                bot = (List<TrfPlayer>)newBot;
                matchingKind = PairingReason.CrossHalfInterchange;
            }
        }
        else
        {
            // Check if the matching is truly natural (no transposition).
            var isNatural = true;
            for (var i = 0; i < half; i++)
            {
                if (assignment[i].PairNumber != bot[i].PairNumber)
                { isNatural = false; break; }
            }
            if (!isNatural) matchingKind = PairingReason.TranspositionAvoidRematch;
        }

        var selectedPairs = Enumerable.Range(0, half)
            .Select(i => (A: top[i], B: assignment[i]))
            .ToList();

        var pairablePool = pool.Take(pairableCount).ToList();
        var scoreCounts = pairablePool
            .GroupBy(ComputeScore)
            .OrderByDescending(g => g.Key)
            .Select(g => g.Count())
            .ToArray();
        var isSingleFloaterEightPlayerPool = pairablePool.Count == 8
            && scoreCounts.Length == 2
            && scoreCounts[0] == 1;

        // USCF 28F2: color optimizations must preserve floater pairings.
        // Floaters sit at bot[0..floaterCount-1] paired with top[0..floaterCount-1].
        if (isSingleFloaterEightPlayerPool &&
            TryFindColorOptimizedMatching(pairablePool, selectedPairs, initialColor, startBoard, out var colorOptimizedPairs, floaterCount))
        {
            selectedPairs = colorOptimizedPairs;
            matchingKind = PairingReason.ColorOptimizedMatching;
        }
        else if (TryReduceColorConflicts(pairablePool, selectedPairs, out var reducedConflictPairs, 0))
        {
            selectedPairs = reducedConflictPairs;
            if (matchingKind == PairingReason.NaturalSlide)
                matchingKind = PairingReason.ColorConflictReduction;
        }

        // After color optimization the floater pair may have moved away
        // from position 0. Floaters come from a higher score group and
        // must retain top-board priority. Move any pair containing a
        // floater back to the front (preserving relative order among
        // floater pairs and among non-floater pairs).
        if (floaterCount > 0)
        {
            var floaterSet = new HashSet<int>(
                bot.Take(floaterCount).Select(f => f.PairNumber));
            var floaterPairs = selectedPairs
                .Where(p => floaterSet.Contains(p.B.PairNumber))
                .ToList();
            var nonFloaterPairs = selectedPairs
                .Where(p => !floaterSet.Contains(p.B.PairNumber))
                .ToList();
            if (floaterPairs.Count > 0 && selectedPairs.IndexOf(floaterPairs[0]) != 0)
            {
                selectedPairs = floaterPairs.Concat(nonFloaterPairs).ToList();
            }
        }

        for (var i = 0; i < selectedPairs.Count; i++)
        {
            var (topPlayer, bottomPlayer) = selectedPairs[i];
            var topGetsWhite = TopGetsWhite(topPlayer, bottomPlayer, initialColor, board);
            var (white, black) = topGetsWhite ? (topPlayer, bottomPlayer) : (bottomPlayer, topPlayer);
            pairings.Add(new UscfPairing(white.PairNumber, black.PairNumber, Board: board));

            // Emit annotations for this board
            var detail = matchingKind switch
            {
                PairingReason.NaturalSlide => $"Natural SLIDE: #{topPlayer.PairNumber} vs #{bottomPlayer.PairNumber}",
                PairingReason.TranspositionAvoidRematch => $"Transposition (USCF 28L1-L2) to avoid rematch: #{topPlayer.PairNumber} vs #{bottomPlayer.PairNumber}",
                PairingReason.CrossHalfInterchange => $"Cross-half interchange (USCF 28L3): #{topPlayer.PairNumber} vs #{bottomPlayer.PairNumber}",
                PairingReason.ColorOptimizedMatching => $"Color-optimized matching: #{topPlayer.PairNumber} vs #{bottomPlayer.PairNumber}",
                PairingReason.ColorConflictReduction => $"Color-conflict reduction (USCF 29E): #{topPlayer.PairNumber} vs #{bottomPlayer.PairNumber}",
                PairingReason.FallbackRematchAccepted => $"Fallback (no non-rematch possible): #{topPlayer.PairNumber} vs #{bottomPlayer.PairNumber} — rematch accepted",
                _ => $"#{topPlayer.PairNumber} vs #{bottomPlayer.PairNumber}",
            };
            annotations.Add(new PairingAnnotation(board, matchingKind, detail));

            // Color annotation
            var colorReason = DescribeColorChoice(topPlayer, bottomPlayer, topGetsWhite, initialColor, board);
            annotations.Add(new PairingAnnotation(board, colorReason.Reason, colorReason.Detail));

            board++;
        }

        return board;
    }

    /// <summary>
    /// Produces a human-readable annotation explaining why a particular
    /// player was assigned White for a given board.
    /// </summary>
    private static (PairingReason Reason, string Detail) DescribeColorChoice(
        TrfPlayer topPlayer, TrfPlayer bottomPlayer, bool topGetsWhite, char initialColor, int board)
    {
        var white = topGetsWhite ? topPlayer : bottomPlayer;
        var black = topGetsWhite ? bottomPlayer : topPlayer;

        // Determine the dominant factor from TopGetsWhite logic.
        var topWhites = topPlayer.Rounds.Count(h => h.Color == 'w');
        var topBlacks = topPlayer.Rounds.Count(h => h.Color == 'b');
        var botWhites = bottomPlayer.Rounds.Count(h => h.Color == 'w');
        var botBlacks = bottomPlayer.Rounds.Count(h => h.Color == 'b');
        var topBalance = topWhites - topBlacks; // positive = more whites
        var botBalance = botWhites - botBlacks;

        if (topBalance != botBalance)
        {
            return (PairingReason.ColorEqualization,
                $"White: #{white.PairNumber} — color equalization (balance: #{topPlayer.PairNumber}={topBalance:+0;-0;0}, #{bottomPlayer.PairNumber}={botBalance:+0;-0;0})");
        }

        // Check alternation (last-round color)
        var topLast = topPlayer.Rounds.Count > 0 ? topPlayer.Rounds[^1].Color : '\0';
        var botLast = bottomPlayer.Rounds.Count > 0 ? bottomPlayer.Rounds[^1].Color : '\0';
        if (topLast != botLast && topLast != '\0' && botLast != '\0')
        {
            return (PairingReason.ColorAlternation,
                $"White: #{white.PairNumber} — alternation (#{topPlayer.PairNumber} was {topLast} last, #{bottomPlayer.PairNumber} was {botLast} last)");
        }

        if (topPlayer.Rating != bottomPlayer.Rating)
        {
            return (PairingReason.ColorByRating,
                $"White: #{white.PairNumber} — higher-rated player gets due color (#{topPlayer.PairNumber}={topPlayer.Rating} vs #{bottomPlayer.PairNumber}={bottomPlayer.Rating})");
        }

        return (PairingReason.ColorByInitialRule,
            $"White: #{white.PairNumber} — initial color rule for board {board}");
    }

    /// <summary>
    /// For small score-group pools, search all non-rematch matchings and
    /// prefer the one that satisfies colour due claims in rating/pairing
    /// order. This models the SwissSys 11 behaviour visible in the 9th
    /// Massachusetts Senior Open Open R4 1.5-score group: instead of
    /// accepting the first rematch-free top-half-vs-bottom-half assignment,
    /// SwissSys interchanges across the group to give the highest players
    /// their due colours when a clean matching exists.
    /// </summary>
    private static bool TryFindColorOptimizedMatching(
        IReadOnlyList<TrfPlayer> pool,
        IReadOnlyList<(TrfPlayer A, TrfPlayer B)> currentPairs,
        char initialColor,
        int startBoard,
        out List<(TrfPlayer A, TrfPlayer B)> optimizedPairs,
        int lockedPairs = 0)
    {
        optimizedPairs = new List<(TrfPlayer A, TrfPlayer B)>();

        // Exhaustive perfect-matching search grows quickly. The groups
        // where SwissSys differs because of colour-due interchanges are
        // usually small; cap this pass to keep the pairer predictable.
        if (pool.Count is 0 or > 8 || pool.Count % 2 != 0)
        {
            return false;
        }

        // USCF 28F2: lock floater pairs — remove them from search space
        // and pre-insert them into results.
        var half = pool.Count / 2;
        var lockedResults = new List<(TrfPlayer A, TrfPlayer B)>();
        var searchIndices = new List<int>();
        for (var i = 0; i < pool.Count; i++)
        {
            // Floater positions: top[0..lockedPairs-1] and bot[0..lockedPairs-1]
            // In pool layout: indices 0..lockedPairs-1 (top) and half..half+lockedPairs-1 (bot)
            if (i < lockedPairs || (i >= half && i < half + lockedPairs))
                continue;
            searchIndices.Add(i);
        }
        for (var i = 0; i < lockedPairs && i < half; i++)
            lockedResults.Add((pool[i], pool[i + half]));

        if (searchIndices.Count == 0)
        {
            // All pairs are locked — nothing to optimize.
            return false;
        }

        var currentOrdered = OrderPairsForBoards(currentPairs, pool);
        var currentScore = ScorePairSet(currentOrdered, pool, initialColor, startBoard);

        List<(TrfPlayer A, TrfPlayer B)>? best = null;
        int[]? bestScore = null;
        var remaining = searchIndices;
        var scratch = new List<(TrfPlayer A, TrfPlayer B)>(lockedResults);

        Search(remaining);

        if (best is null || bestScore is null || CompareScores(bestScore, currentScore) <= 0)
        {
            return false;
        }

        optimizedPairs = best;
        return true;

        void Search(List<int> rem)
        {
            if (rem.Count == 0)
            {
                var ordered = OrderPairsForBoards(scratch, pool);
                var score = ScorePairSet(ordered, pool, initialColor, startBoard);
                if (bestScore is null || CompareScores(score, bestScore) > 0)
                {
                    bestScore = score;
                    best = ordered.ToList();
                }
                return;
            }

            var first = rem[0];
            for (var k = 1; k < rem.Count; k++)
            {
                var second = rem[k];
                var a = pool[first];
                var b = pool[second];
                if (IsForbiddenPair(a, b)) continue;

                scratch.Add((a, b));
                var next = new List<int>(rem.Count - 2);
                for (var i = 1; i < rem.Count; i++)
                {
                    if (i != k) next.Add(rem[i]);
                }
                Search(next);
                scratch.RemoveAt(scratch.Count - 1);
            }
        }
    }

    private static List<(TrfPlayer A, TrfPlayer B)> OrderPairsForBoards(
        IReadOnlyList<(TrfPlayer A, TrfPlayer B)> pairs,
        IReadOnlyList<TrfPlayer> pool)
    {
        return pairs
            .OrderByDescending(p => Math.Max(ComputeScore(p.A), ComputeScore(p.B)))
            .ThenByDescending(p => Math.Max(p.A.Rating, p.B.Rating))
            .ThenByDescending(p => Math.Min(p.A.Rating, p.B.Rating))
            .ToList();
    }

    private static int[] ScorePairSet(
        IReadOnlyList<(TrfPlayer A, TrfPlayer B)> pairs,
        IReadOnlyList<TrfPlayer> pool,
        char initialColor,
        int startBoard)
    {
        var assigned = new Dictionary<int, char>();
        for (var i = 0; i < pairs.Count; i++)
        {
            var (a, b) = pairs[i];
            var aGetsWhite = TopGetsWhite(a, b, initialColor, startBoard + i, useSamePreferenceScoreTie: false);
            assigned[a.PairNumber] = aGetsWhite ? 'w' : 'b';
            assigned[b.PairNumber] = aGetsWhite ? 'b' : 'w';
        }

        // Lexicographic score by pool order first: satisfying the highest
        // player in the score group beats satisfying only lower players.
        // Then preserve the Swiss SLIDE shape player-by-player in pool
        // order, prefer total satisfaction, then total slide closeness.
        var half = pool.Count / 2;
        var score = new int[pool.Count + (half * 2) + 2];
        var total = 0;
        var slidePenalty = 0;
        var indexByPair = pool
            .Select((p, i) => (p.PairNumber, Index: i))
            .ToDictionary(x => x.PairNumber, x => x.Index);
        for (var i = 0; i < pool.Count; i++)
        {
            var p = pool[i];
            var pref = PreferredColor(p);
            if (pref != '-' && assigned.TryGetValue(p.PairNumber, out var got) && got == pref)
            {
                score[i] = 1;
                total++;
            }
        }
        var slot = pool.Count;
        for (var i = 0; i < half; i++)
        {
            var partner = FindPartnerIndex(pool[i].PairNumber, pairs, indexByPair);
            var delta = partner - (i + half);
            score[slot++] = -Math.Abs(delta);
            score[slot++] = delta;
        }
        foreach (var (a, b) in pairs)
        {
            slidePenalty += SlidePenalty(indexByPair[a.PairNumber], indexByPair[b.PairNumber], half);
        }
        score[^2] = total;
        score[^1] = -slidePenalty;
        return score;
    }

    private static int FindPartnerIndex(
        int pairNumber,
        IReadOnlyList<(TrfPlayer A, TrfPlayer B)> pairs,
        IReadOnlyDictionary<int, int> indexByPair)
    {
        foreach (var (a, b) in pairs)
        {
            if (a.PairNumber == pairNumber) return indexByPair[b.PairNumber];
            if (b.PairNumber == pairNumber) return indexByPair[a.PairNumber];
        }
        return -1;
    }

    private static int SlidePenalty(int a, int b, int half)
    {
        var lo = Math.Min(a, b);
        var hi = Math.Max(a, b);
        if (lo < half)
        {
            return Math.Abs(hi - (lo + half));
        }

        // Both players came from the original bottom half. This only
        // happens after colour-driven interchanges; keep it neutral so
        // the earlier pairs decide the SwissSys-style tiebreak.
        return 0;
    }

    private static int CompareScores(IReadOnlyList<int> left, IReadOnlyList<int> right)
    {
        var n = Math.Min(left.Count, right.Count);
        for (var i = 0; i < n; i++)
        {
            var cmp = left[i].CompareTo(right[i]);
            if (cmp != 0) return cmp;
        }
        return left.Count.CompareTo(right.Count);
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

    private static int CountColorConflictPairs(IReadOnlyList<(TrfPlayer A, TrfPlayer B)> pairs)
    {
        var n = 0;
        foreach (var (a, b) in pairs)
        {
            var prefA = PreferredColor(a);
            var prefB = PreferredColor(b);
            if (prefA != '-' && prefA == prefB) n++;
        }
        return n;
    }

    /// <summary>
    /// USCF 29E (narrow scope): search within the score-group pool's
    /// top-half × bottom-half bipartite shape for an alternative
    /// rematch-free matching with STRICTLY FEWER hard colour conflicts
    /// (pairs where both players prefer the same colour) than the
    /// natural SLIDE pairing supplied in <paramref name="currentPairs"/>.
    /// </summary>
    /// <remarks>
    /// <para>Search shape preserves the SLIDE structure SwissSys uses:
    /// for each top[i], try every bot[j] not yet used. This is the
    /// same shape as <see cref="TryFindNonRematchMatching"/>; we
    /// enumerate all complete bipartite matchings instead of just
    /// the first rematch-free one, and pick the lowest-conflict
    /// answer.</para>
    /// <para>Ties on conflict count break by minimum total bottom-half
    /// disturbance (sum of <c>|j - i|</c>), which favours the natural
    /// pairing whenever it's already conflict-free or among the
    /// lowest-conflict candidates — so we never gratuitously re-shuffle
    /// when the natural answer is fine.</para>
    /// <para>No size cap: the search is branch-and-bound on conflict
    /// count and disturbance, so as soon as a zero-conflict matching
    /// is found the whole rest of the tree is pruned. Real-world
    /// USCF score groups (even 30-player Open sections) terminate
    /// in well under a millisecond.</para>
    /// </remarks>
    private static bool TryReduceColorConflicts(
        IReadOnlyList<TrfPlayer> pool,
        IReadOnlyList<(TrfPlayer A, TrfPlayer B)> currentPairs,
        out List<(TrfPlayer A, TrfPlayer B)> reducedPairs,
        int lockedPairs = 0)
    {
        reducedPairs = new List<(TrfPlayer A, TrfPlayer B)>();
        if (pool.Count == 0 || pool.Count % 2 != 0) return false;

        var currentConflicts = CountColorConflictPairs(currentPairs);
        if (currentConflicts == 0) return false;

        var half = pool.Count / 2;
        var top = new TrfPlayer[half];
        var bot = new TrfPlayer[half];
        for (var i = 0; i < half; i++)
        {
            top[i] = pool[i];
            bot[i] = pool[i + half];
        }

        var used = new bool[half];
        var assignment = new TrfPlayer[half];
        var bestConflicts = currentConflicts;
        var bestDisturbance = int.MaxValue;
        TrfPlayer[]? best = null;

        // USCF 28F2: lock floater positions — bot[i] must stay with top[i]
        // for i < lockedPairs.
        var initialConflicts = 0;
        var initialDisturbance = 0;
        for (var i = 0; i < lockedPairs && i < half; i++)
        {
            used[i] = true;
            assignment[i] = bot[i];
            var prefA = PreferredColor(top[i]);
            var prefB = PreferredColor(bot[i]);
            if (prefA != '-' && prefA == prefB) initialConflicts++;
        }

        Search(lockedPairs, initialConflicts, initialDisturbance);

        if (best is null) return false;

        var list = new List<(TrfPlayer A, TrfPlayer B)>(half);
        for (var i = 0; i < half; i++) list.Add((top[i], best[i]));
        reducedPairs = list;
        return true;

        void Search(int i, int conflictsSoFar, int disturbanceSoFar)
        {
            if (conflictsSoFar > bestConflicts) return;
            if (conflictsSoFar == bestConflicts && disturbanceSoFar >= bestDisturbance) return;
            if (i == half)
            {
                if (conflictsSoFar < bestConflicts ||
                    (conflictsSoFar == bestConflicts && disturbanceSoFar < bestDisturbance))
                {
                    bestConflicts = conflictsSoFar;
                    bestDisturbance = disturbanceSoFar;
                    best = (TrfPlayer[])assignment.Clone();
                }
                return;
            }

            for (var j = 0; j < half; j++)
            {
                if (used[j]) continue;
                var a = top[i];
                var b = bot[j];
                if (IsForbiddenPair(a, b)) continue;

                var prefA = PreferredColor(a);
                var prefB = PreferredColor(b);
                var conflict = prefA != '-' && prefA == prefB ? 1 : 0;

                used[j] = true;
                assignment[i] = b;
                Search(i + 1, conflictsSoFar + conflict, disturbanceSoFar + Math.Abs(j - i));
                used[j] = false;
            }
        }
    }

    /// <summary>
    /// True when at least one pair in <paramref name="pairs"/> has both
    /// players preferring the same colour — a hard colour conflict that
    /// the round's colour assignment will be unable to satisfy without
    /// re-matching. Used to gate the USCF 29E colour-driven transposition
    /// search so it only runs when the natural SLIDE pairing actually
    /// has a conflict to resolve.
    /// </summary>
    private static bool HasColorConflictPair(IReadOnlyList<(TrfPlayer A, TrfPlayer B)> pairs)
    {
        foreach (var (a, b) in pairs)
        {
            var prefA = PreferredColor(a);
            var prefB = PreferredColor(b);
            if (prefA != '-' && prefA == prefB) return true;
        }
        return false;
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
    private static bool TopGetsWhite(
        TrfPlayer top,
        TrfPlayer bottom,
        char initialColor,
        int board,
        bool useSamePreferenceScoreTie = true)
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

        var topPreferred = PreferredColor(top);
        var botPreferred = PreferredColor(bottom);
        if (useSamePreferenceScoreTie && topPreferred != '-' && topPreferred == botPreferred)
        {
            var topScore = ComputeScore(top);
            var botScore = ComputeScore(bottom);
            if (topScore > botScore) return topPreferred == 'w';
            if (botScore > topScore) return botPreferred == 'b';
        }

        // (2) Equalise: whoever has played more whites gets black.
        var topDiff = CountColor(top, 'w') - CountColor(top, 'b');
        var botDiff = CountColor(bottom, 'w') - CountColor(bottom, 'b');
        if (topDiff < botDiff) return true;   // top has fewer whites
        if (topDiff > botDiff) return false;  // top has more whites

        // (3) Alternate: last actual-game colour decides.
        var topLast = LastColor(top);
        var botLast = LastColor(bottom);
        if (topLast != '-' && botLast != '-')
        {
            // Both have history — alternate based on top's last.
            return topLast == 'w' ? false : true;
        }
        if (topLast != '-')
        {
            return topLast == 'w' ? false : true;
        }
        if (botLast != '-')
        {
            // Only bottom has history — honour bottom's alternation.
            // bot had white last → bot due black → top gets white (true)
            // bot had black last → bot due white → top gets black (false)
            return botLast == 'w';
        }

        // (4) Neither player has any colour history (both had byes or
        //     forfeits in all prior rounds). When their scores differ the
        //     higher-scored player's "due colour" takes priority — it is
        //     inferred from the initial-colour pattern of their score
        //     group (matching SwissSys behaviour). The higher-scored
        //     player is treated as top-half of their natural score group
        //     and assigned the board-1 colour for the CURRENT round
        //     (which alternates each round from the initial colour).
        var topPts = ComputeScore(top);
        var botPts = ComputeScore(bottom);
        if (topPts != botPts)
        {
            // Current round = number of prior rounds + 1.
            var currentRound = top.Rounds.Count + 1;
            // In odd rounds top-half gets initialColor on board 1;
            // in even rounds top-half gets the opposite.
            var topHalfGetsWhiteThisRound = (initialColor == 'w') ^ (currentRound % 2 == 0);
            var higherIsTop = topPts > botPts;
            // If the higher-scored player IS "top" → they get topHalfGetsWhiteThisRound.
            // Otherwise flip.
            return higherIsTop == topHalfGetsWhiteThisRound;
        }

        return InitialColorOnBoard(initialColor, board); // (5) R1 fallback
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

    private static char PreferredColor(TrfPlayer p)
    {
        var streak = StreakColor(p);
        if (streak == 'w') return 'b';
        if (streak == 'b') return 'w';

        var diff = CountColor(p, 'w') - CountColor(p, 'b');
        if (diff > 0) return 'b';
        if (diff < 0) return 'w';

        return LastColor(p) switch
        {
            'w' => 'b',
            'b' => 'w',
            _   => '-',
        };
    }
}
