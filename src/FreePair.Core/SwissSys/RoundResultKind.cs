namespace FreePair.Core.SwissSys;

/// <summary>
/// Kind of a single round result as recorded in a SwissSys <c>.sjson</c>
/// player history token.
/// </summary>
public enum RoundResultKind
{
    /// <summary>No game / uninitialized slot (<c>~</c> or <c>\u0000</c>).</summary>
    None = 0,

    /// <summary>Win (<c>+</c>).</summary>
    Win,

    /// <summary>Loss (<c>-</c>).</summary>
    Loss,

    /// <summary>Draw (<c>=</c>).</summary>
    Draw,

    /// <summary>Full-point bye (<c>B</c>).</summary>
    FullPointBye,

    /// <summary>Half-point bye (<c>H</c>).</summary>
    HalfPointBye,

    /// <summary>
    /// Zero-point bye (<c>U</c> in SwissSys convention): the player
    /// was unpaired for this round and received no points. Distinct
    /// from <see cref="None"/> (uninitialized slot) in that the TD
    /// affirmatively assigned this — e.g. a late entry, a withdrawal
    /// round, or an odd-count odd-one-out the TD decided not to give
    /// a full point.
    /// </summary>
    ZeroPointBye,
}
