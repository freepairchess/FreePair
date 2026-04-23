using System;
using System.Collections.Generic;
using System.Linq;

namespace FreePair.Core.Tournaments.Constraints;

/// <summary>
/// Flags pairings listed on a TD-maintained "do not pair" blacklist
/// (siblings, teammates who've recently drilled a position, previous-
/// round arbiter-observed conflict, etc.). Pair-number pairs are stored
/// unordered.
/// </summary>
public sealed class DoNotPairConstraint : IPairingConstraint
{
    private readonly HashSet<(int Lo, int Hi)> _pairs;

    /// <summary>
    /// Builds a constraint from an enumerable of unordered pair-number
    /// tuples. Duplicates are collapsed; self-pairs <c>(n, n)</c> are
    /// silently dropped.
    /// </summary>
    public DoNotPairConstraint(IEnumerable<(int A, int B)> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        _pairs = pairs
            .Where(p => p.A != p.B)
            .Select(p => (Lo: Math.Min(p.A, p.B), Hi: Math.Max(p.A, p.B)))
            .ToHashSet();
    }

    public bool Violates(Player a, Player b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var key = a.PairNumber < b.PairNumber
            ? (a.PairNumber, b.PairNumber)
            : (b.PairNumber, a.PairNumber);
        return _pairs.Contains(key);
    }

    public string Describe(Player a, Player b) =>
        $"do-not-pair list (#{a.PairNumber}/#{b.PairNumber})";
}
