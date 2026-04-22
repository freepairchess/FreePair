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
}
