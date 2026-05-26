using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Bbp;
using FreePair.Core.Tournaments.Constraints;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Result of applying <see cref="PairingSwapper.Apply"/>: the (possibly
/// rearranged) pairings plus a list of human-readable reasons for any
/// violations that could not be resolved via a valid swap.
/// </summary>
public sealed record PairingSwapResult(
    IReadOnlyList<BbpPairing> Pairings,
    IReadOnlyList<string> UnresolvedConflicts);

/// <summary>
/// Post-processes pairing-engine output to honour TD-supplied
/// <see cref="IPairingConstraint"/>s (same-team, same-club, do-not-pair).
/// When a pairing violates a constraint, the swapper searches all other
/// pairings in the same score group for a swap that resolves the conflict
/// without introducing new ones. Among valid candidates it picks the swap
/// that minimises <b>rating displacement</b> — the sum of absolute rating
/// differences between the old and new opponents — so that pairing quality
/// stays as close to the engine's original output as possible.
/// </summary>
public static class PairingSwapper
{
    public static PairingSwapResult Apply(
        IReadOnlyList<BbpPairing> pairings,
        Section section,
        IReadOnlyList<IPairingConstraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(pairings);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(constraints);

        if (constraints.Count == 0 || pairings.Count == 0)
        {
            return new PairingSwapResult(pairings, Array.Empty<string>());
        }

        var byPair = section.Players.ToDictionary(p => p.PairNumber);
        var pastOpponents = section.Players.ToDictionary(
            p => p.PairNumber,
            p => new HashSet<int>(
                p.History.Select(h => h.Opponent).Where(o => o > 0)));

        var working = pairings.ToArray();

        // Multiple passes: resolving one violation may unblock another.
        const int maxPasses = 10;
        for (var pass = 0; pass < maxPasses; pass++)
        {
            var madeSwap = false;
            for (var i = 0; i < working.Length; i++)
            {
                var current = working[i];
                if (!byPair.TryGetValue(current.WhitePair, out var whiteI) ||
                    !byPair.TryGetValue(current.BlackPair, out var blackI))
                {
                    continue;
                }

                if (FirstViolated(whiteI, blackI, constraints) is null) continue;

                // Find the best swap across all other boards in the same
                // score group. We try four swap variants per candidate
                // board and pick the one with the lowest rating cost.
                int bestJ = -1;
                SwapKind bestKind = default;
                int bestCost = int.MaxValue;

                for (var j = 0; j < working.Length; j++)
                {
                    if (j == i) continue;
                    var cand = working[j];
                    if (!byPair.TryGetValue(cand.WhitePair, out var whiteJ) ||
                        !byPair.TryGetValue(cand.BlackPair, out var blackJ))
                    {
                        continue;
                    }

                    // Same-score-group check (preserves Swiss bracket quality).
                    if (whiteI.Score != whiteJ.Score || blackI.Score != blackJ.Score)
                    {
                        // Also try the transposed score check for cross-colour swaps.
                        if (whiteI.Score != blackJ.Score || blackI.Score != whiteJ.Score)
                            continue;

                        // Cross-score match: only cross-colour swaps are valid here.
                        TrySwap(SwapKind.CrossAB, whiteI, blackI, whiteJ, blackJ, i, j, ref bestJ, ref bestKind, ref bestCost);
                        TrySwap(SwapKind.CrossBA, whiteI, blackI, whiteJ, blackJ, i, j, ref bestJ, ref bestKind, ref bestCost);
                        continue;
                    }

                    // Standard score match: try all four swap kinds.
                    TrySwap(SwapKind.SwapBlacks, whiteI, blackI, whiteJ, blackJ, i, j, ref bestJ, ref bestKind, ref bestCost);
                    TrySwap(SwapKind.SwapWhites, whiteI, blackI, whiteJ, blackJ, i, j, ref bestJ, ref bestKind, ref bestCost);
                    TrySwap(SwapKind.CrossAB, whiteI, blackI, whiteJ, blackJ, i, j, ref bestJ, ref bestKind, ref bestCost);
                    TrySwap(SwapKind.CrossBA, whiteI, blackI, whiteJ, blackJ, i, j, ref bestJ, ref bestKind, ref bestCost);
                }

                if (bestJ >= 0)
                {
                    ApplySwap(working, i, bestJ, bestKind, byPair);
                    madeSwap = true;
                }
            }

            if (!madeSwap) break;
        }

        // Final pass: collect remaining unresolved conflicts.
        var unresolved = new List<string>();
        for (var i = 0; i < working.Length; i++)
        {
            var current = working[i];
            if (!byPair.TryGetValue(current.WhitePair, out var whiteI) ||
                !byPair.TryGetValue(current.BlackPair, out var blackI))
            {
                continue;
            }

            var violated = FirstViolated(whiteI, blackI, constraints);
            if (violated is not null)
            {
                unresolved.Add(
                    $"#{current.WhitePair} vs #{current.BlackPair}: {violated.Describe(whiteI, blackI)}");
            }
        }

        return new PairingSwapResult(working, unresolved);

        // === Local functions ===

        void TrySwap(SwapKind kind,
            Player whiteI, Player blackI, Player whiteJ, Player blackJ,
            int idxI, int idxJ,
            ref int rBestJ, ref SwapKind rBestKind, ref int rBestCost)
        {
            // Determine the two resulting pairings (newWhiteI vs newBlackI)
            // and (newWhiteJ vs newBlackJ) for each swap kind.
            Player newWI, newBI, newWJ, newBJ;
            switch (kind)
            {
                case SwapKind.SwapBlacks:
                    // (Wi, Bj) and (Wj, Bi)
                    newWI = whiteI; newBI = blackJ; newWJ = whiteJ; newBJ = blackI;
                    break;
                case SwapKind.SwapWhites:
                    // (Wj, Bi) and (Wi, Bj) — symmetric to SwapBlacks but whites move
                    newWI = whiteJ; newBI = blackI; newWJ = whiteI; newBJ = blackJ;
                    break;
                case SwapKind.CrossAB:
                    // Wi takes Bj's spot, Bj takes Wi's spot → (Bj, Bi) and (Wj, Wi)
                    // Actually: swap Wi with Bj across boards.
                    newWI = blackJ; newBI = blackI; newWJ = whiteJ; newBJ = whiteI;
                    break;
                case SwapKind.CrossBA:
                    // Swap Bi with Wj across boards: (Wi, Wj) and (Bi, Bj)
                    newWI = whiteI; newBI = whiteJ; newWJ = blackI; newBJ = blackJ;
                    break;
                default:
                    return;
            }

            // Both resulting pairings must be constraint-free.
            if (FirstViolated(newWI, newBI, constraints) is not null) return;
            if (FirstViolated(newWJ, newBJ, constraints) is not null) return;

            // Must not recreate a previously-played game.
            if (pastOpponents[newWI.PairNumber].Contains(newBI.PairNumber)) return;
            if (pastOpponents[newWJ.PairNumber].Contains(newBJ.PairNumber)) return;

            // Displacement cost: how far did opponents move from the
            // engine's original assignments? We sum the absolute rating
            // differences between original and new opponents for all four
            // affected players. Lower means closer to engine output.
            //
            // Board distance: USCF rules prefer swapping with the nearest-
            // ranked alternative.
            //
            // Direction: USCF prefers swapping with the next player DOWN
            // (lower-rated / higher board index) before trying upward.
            var cost = Math.Abs(blackI.Rating - newBI.Rating)
                     + Math.Abs(blackJ.Rating - newBJ.Rating)
                     + Math.Abs(whiteI.Rating - newWI.Rating)
                     + Math.Abs(whiteJ.Rating - newWJ.Rating);

            // Prefer swaps with nearby boards.
            cost += Math.Abs(idxI - idxJ) * 1000;

            // Prefer swapping downward (j > i) per USCF transposition rules.
            if (idxJ < idxI)
                cost += 200;

            if (cost < rBestCost)
            {
                rBestJ = idxJ;
                rBestKind = kind;
                rBestCost = cost;
            }
        }
    }

    private static void ApplySwap(BbpPairing[] working, int i, int j, SwapKind kind,
        IReadOnlyDictionary<int, Player> byPair)
    {
        var pi = working[i];
        var pj = working[j];
        var wI = byPair[pi.WhitePair];
        var bI = byPair[pi.BlackPair];
        var wJ = byPair[pj.WhitePair];
        var bJ = byPair[pj.BlackPair];

        switch (kind)
        {
            case SwapKind.SwapBlacks:
                working[i] = new BbpPairing(wI.PairNumber, bJ.PairNumber);
                working[j] = new BbpPairing(wJ.PairNumber, bI.PairNumber);
                break;
            case SwapKind.SwapWhites:
                working[i] = new BbpPairing(wJ.PairNumber, bI.PairNumber);
                working[j] = new BbpPairing(wI.PairNumber, bJ.PairNumber);
                break;
            case SwapKind.CrossAB:
                working[i] = new BbpPairing(bJ.PairNumber, bI.PairNumber);
                working[j] = new BbpPairing(wJ.PairNumber, wI.PairNumber);
                break;
            case SwapKind.CrossBA:
                working[i] = new BbpPairing(wI.PairNumber, wJ.PairNumber);
                working[j] = new BbpPairing(bI.PairNumber, bJ.PairNumber);
                break;
        }
    }

    private enum SwapKind { SwapBlacks, SwapWhites, CrossAB, CrossBA }

    private static IPairingConstraint? FirstViolated(
        Player a,
        Player b,
        IReadOnlyList<IPairingConstraint> constraints)
    {
        foreach (var c in constraints)
        {
            if (c.Violates(a, b)) return c;
        }
        return null;
    }
}
