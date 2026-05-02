using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Uscf.Trf;

namespace FreePair.Core.Uscf;

/// <summary>
/// FreePair's USCF Swiss pairing engine. Phase 0: round-1 pairing only —
/// USCF Official Rules of Chess §28C "Pairing the first round".
/// </summary>
/// <remarks>
/// <para><b>Round 1 algorithm (this phase):</b></para>
/// <list type="number">
///   <item>Filter the player roster to those active for the next round
///         (pair number positive; no per-round filtering needed yet because
///         requested-bye / withdrawn handling is wired in via the TRF cells
///         in P4).</item>
///   <item>Sort by rating descending, breaking ties by pair number
///         ascending (deterministic, matches the order TRF emits).</item>
///   <item>If the count is odd, the lowest-rated player gets a full-point
///         bye and is removed from the pairing pool.</item>
///   <item>Split the remaining players in half. Pair seed[i] with
///         seed[i + half] for i = 0..half-1 — the standard "top half plays
///         bottom half" Swiss pairing of round 1.</item>
///   <item>Allocate colours: top seed of board 1 receives the
///         <see cref="TrfDocument.InitialColor"/> (XXC). Subsequent boards
///         alternate, so half the top seeds get white and half black.</item>
/// </list>
/// <para>Round 2+ is rejected with <see cref="NotImplementedException"/> for
/// now; that's the next phase of the build.</para>
/// </remarks>
public static class UscfPairer
{
    /// <summary>
    /// Pairs the next round of <paramref name="document"/>.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// Thrown when any player has played-round history (round 2+ pairing is
    /// not yet implemented in this phase of the engine).
    /// </exception>
    public static UscfPairingResult Pair(TrfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Determine how many rounds of history we have. A "real" round
        // cell is one where the player either has an opponent or a bye
        // result. Trailing pre-flagged half-byes (Opponent==0 && 'H')
        // also count as history for our purposes — the corresponding
        // player should be skipped when pairing the upcoming round.
        var maxPlayedRounds = document.Players.Count == 0
            ? 0
            : document.Players.Max(CountPlayedRounds);

        if (maxPlayedRounds > 0)
        {
            // Round 2+ pairing is the next phase of the build. We surface
            // a clear error rather than silently producing a wrong answer.
            throw new NotImplementedException(
                "FreePair.UscfEngine currently supports round-1 pairing only. " +
                "Round 2+ (score-group pairing, color allocation, repeat-pairing avoidance) " +
                "is the next phase. Track progress in docs/USCF_ENGINE.md.");
        }

        return PairRoundOne(document);
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
}
