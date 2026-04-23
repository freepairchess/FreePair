namespace FreePair.Core.Tournaments.Enums;

/// <summary>
/// Broad time-control category. Values mirror NAChessHub's
/// <c>TimeControlType</c> enum numerically. The free-form
/// <see cref="Tournament.TimeControl"/> string still carries the
/// exact spec (e.g. <c>G/45;d5</c>); this enum is for filtering /
/// display / ratings-system selection.
/// </summary>
public enum TimeControlType
{
    Bullet = 0,
    Blitz = 1,
    Rapid = 2,
    /// <summary>Dual-rated rapid + classical.</summary>
    RapidAndClassical = 3,
    Classical = 4,
    Other = 5,
}
