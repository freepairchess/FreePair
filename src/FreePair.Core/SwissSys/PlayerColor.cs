namespace FreePair.Core.SwissSys;

/// <summary>
/// Color (side) a player took in a single game.
/// </summary>
public enum PlayerColor
{
    /// <summary>No color assigned (byes, unpaired, uninitialized).</summary>
    None = 0,

    /// <summary>Played the White pieces.</summary>
    White,

    /// <summary>Played the Black pieces.</summary>
    Black,
}
