namespace FreePair.Core.Tournaments.Tiebreaks;

/// <summary>
/// Standard US Chess Federation tiebreak systems supported by FreePair.
/// </summary>
public enum TiebreakSystem
{
    /// <summary>Modified Median (USCF). Sum of opponent-adjusted scores with drops.</summary>
    ModifiedMedian,

    /// <summary>Solkoff. Sum of opponent-adjusted scores with no drops.</summary>
    Solkoff,

    /// <summary>Cumulative. Sum of running scores, minus bye-point adjustments.</summary>
    Cumulative,

    /// <summary>Opponent Cumulative. Sum of each opponent's Cumulative score.</summary>
    OpponentCumulative,
}
