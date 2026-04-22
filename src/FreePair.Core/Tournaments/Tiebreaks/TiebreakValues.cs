namespace FreePair.Core.Tournaments.Tiebreaks;

/// <summary>
/// The standard four USCF tiebreak values computed for a single player.
/// </summary>
public sealed record TiebreakValues(
    decimal ModifiedMedian,
    decimal Solkoff,
    decimal Cumulative,
    decimal OpponentCumulative)
{
    /// <summary>Gets the value for the requested system.</summary>
    public decimal this[TiebreakSystem system] => system switch
    {
        TiebreakSystem.ModifiedMedian => ModifiedMedian,
        TiebreakSystem.Solkoff => Solkoff,
        TiebreakSystem.Cumulative => Cumulative,
        TiebreakSystem.OpponentCumulative => OpponentCumulative,
        _ => 0m,
    };
}
