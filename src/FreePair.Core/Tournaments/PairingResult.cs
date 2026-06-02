namespace FreePair.Core.Tournaments;

/// <summary>
/// Outcome of a played (or scheduled) board pairing.
/// </summary>
public enum PairingResult
{
    /// <summary>Round not yet played; result unknown.</summary>
    Unplayed,

    /// <summary>White won (1-0).</summary>
    WhiteWins,

    /// <summary>Black won (0-1).</summary>
    BlackWins,

    /// <summary>Draw (½-½).</summary>
    Draw,

    /// <summary>White won by forfeit — black forfeited (1-0F).</summary>
    WhiteWinsForfeit,

    /// <summary>Black won by forfeit — white forfeited (0F-1).</summary>
    BlackWinsForfeit,

    /// <summary>Double forfeit — neither player showed (0F-0F).</summary>
    DoubleForfeit,
}
