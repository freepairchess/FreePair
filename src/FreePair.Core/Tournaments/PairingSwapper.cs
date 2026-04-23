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
/// Post-processes BBP's proposed pairings to honour TD-supplied
/// <see cref="IPairingConstraint"/>s (same-team, same-club, do-not-pair
/// blacklist, …). When a pairing violates any active constraint, the
/// swapper searches for another pairing in the <em>same score group</em>
/// — i.e. whose white and black players have the same respective scores
/// as the violating pair — whose colour-stable swap would leave both
/// resulting pairings constraint-free and not previously-played. If no
/// such swap exists the violation is left in place and surfaced via
/// <see cref="PairingSwapResult.UnresolvedConflicts"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Colour stability</strong>: swapping pairings (Aw, Ab) and
/// (Cw, Cb) yields (Aw, Cb) and (Cw, Ab). Every player keeps the colour
/// BBP assigned them, so FIDE C.04 colour preferences continue to hold.
/// </para>
/// <para>
/// <strong>Same-score-group rule</strong>: the swap is only considered
/// when <c>Aw.Score == Cw.Score</c> and <c>Ab.Score == Cb.Score</c>.
/// This preserves the Dutch score-bracket pairing that BBP built, so
/// the post-processing never distorts Swiss quality.
/// </para>
/// <para>
/// <strong>Pass strategy</strong>: a single forward pass over the
/// proposed pairings — each violation is resolved (or left) in isolation.
/// A swap that resolves one conflict but introduces a new one (against
/// a different constraint) is rejected before it is applied. In
/// pathological cases a subsequent conflict may therefore remain
/// unresolved; the TD sees it in the result and can decide.
/// </para>
/// </remarks>
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
        var unresolved = new List<string>();

        for (var i = 0; i < working.Length; i++)
        {
            var current = working[i];
            if (!byPair.TryGetValue(current.WhitePair, out var whiteI) ||
                !byPair.TryGetValue(current.BlackPair, out var blackI))
            {
                continue; // Orphan pair number — shouldn't happen from BBP, skip.
            }

            var violated = FirstViolated(whiteI, blackI, constraints);
            if (violated is null) continue;

            var swapped = false;
            for (var j = 0; j < working.Length && !swapped; j++)
            {
                if (i == j) continue;
                var candidate = working[j];
                if (!byPair.TryGetValue(candidate.WhitePair, out var whiteJ) ||
                    !byPair.TryGetValue(candidate.BlackPair, out var blackJ))
                {
                    continue;
                }

                // Same-score-group check preserves Swiss quality.
                if (whiteI.Score != whiteJ.Score) continue;
                if (blackI.Score != blackJ.Score) continue;

                // Proposed colour-stable rearrangement.
                //   (Aw, Ab), (Cw, Cb)  →  (Aw, Cb), (Cw, Ab)
                if (FirstViolated(whiteI, blackJ, constraints) is not null) continue;
                if (FirstViolated(whiteJ, blackI, constraints) is not null) continue;

                if (pastOpponents[whiteI.PairNumber].Contains(blackJ.PairNumber)) continue;
                if (pastOpponents[whiteJ.PairNumber].Contains(blackI.PairNumber)) continue;

                working[i] = new BbpPairing(whiteI.PairNumber, blackJ.PairNumber);
                working[j] = new BbpPairing(whiteJ.PairNumber, blackI.PairNumber);
                swapped = true;
            }

            if (!swapped)
            {
                unresolved.Add(
                    $"#{current.WhitePair} vs #{current.BlackPair}: {violated.Describe(whiteI, blackI)}");
            }
        }

        return new PairingSwapResult(working, unresolved);
    }

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
