using System;
using System.Collections.Generic;
using System.Linq;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Computes the recommended starting board number for each section
/// in a multi-section tournament. The rule:
/// <list type="bullet">
///   <item>Section 0 starts at board 1.</item>
///   <item>Section <i>n</i> starts at <c>section[n-1].FirstBoard + maxBoardsNeeded(section[n-1])</c>.</item>
/// </list>
/// where <c>maxBoardsNeeded</c> is <c>ceil(activePlayers / 2)</c>.
/// "Active" here means non-soft-deleted, non-withdrawn — i.e. players
/// who could still be paired in any future round. Round-specific
/// byes (half-point, requested) are deliberately ignored: the offset
/// reserves capacity for the worst-case round, so board numbers are
/// stable across rounds even when bye counts fluctuate.
/// </summary>
public static class BoardNumberRecommender
{
    /// <summary>
    /// Returns the recommended <see cref="Section.FirstBoard"/> for
    /// every (non-soft-deleted) section in
    /// <paramref name="tournament"/>, keyed by section name. Caller
    /// can apply selectively (e.g. only update sections whose
    /// current value disagrees) via
    /// <see cref="TournamentMutations.SetSectionFirstBoard"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, int> Recommend(Tournament tournament)
    {
        ArgumentNullException.ThrowIfNull(tournament);

        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        var nextBoard = 1;
        foreach (var section in tournament.Sections.Where(s => !s.SoftDeleted))
        {
            result[section.Name] = nextBoard;
            nextBoard += MaxBoardsNeeded(section);
        }
        return result;
    }

    /// <summary>
    /// Worst-case board count for <paramref name="section"/> across
    /// any round — <c>ceil(activePlayers / 2)</c>. Withdrawn and
    /// soft-deleted players are excluded since they can't be paired.
    /// </summary>
    public static int MaxBoardsNeeded(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);
        var activeCount = section.Players.Count(p => !p.SoftDeleted && !p.Withdrawn);
        return (activeCount + 1) / 2;  // ceil(n/2) for non-negative n
    }
}
