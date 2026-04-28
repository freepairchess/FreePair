using System.Globalization;
using FreePair.Core.SwissSys;

namespace FreePair.Core.UscfExport;

/// <summary>
/// Encodes a single round result into the 7-character format USCF
/// expects in <c>D_RND01..N</c> columns.
/// </summary>
/// <remarks>
/// <para>Format: <c>{Result}{Opponent}{Color}</c>, left-justified
/// in a 7-char space-padded slot. Examples:</para>
/// <list type="bullet">
///   <item><c>"W9B    "</c> — win against opponent #9 with Black.</item>
///   <item><c>"D16W   "</c> — draw with opponent #16 as White.</item>
///   <item><c>"H0     "</c> — half-point bye (no opponent / no colour).</item>
///   <item><c>"B0     "</c> — full-point bye.</item>
///   <item><c>"U0     "</c> — unplayed (round not played by this section yet).</item>
/// </list>
/// </remarks>
public static class UscfRoundCode
{
    public const int Width = 7;

    /// <summary>
    /// Encodes <paramref name="result"/> for output. Use
    /// <see cref="Unplayed"/> when a section's final round is less
    /// than the file's column count (sections that ended early get
    /// <c>"U0     "</c> in the trailing columns).
    /// </summary>
    public static string Encode(RoundResult result)
    {
        var (letter, opponent, colorLetter) = result.Kind switch
        {
            RoundResultKind.Win          => ('W', result.Opponent, ColorLetter(result.Color)),
            RoundResultKind.Loss         => ('L', result.Opponent, ColorLetter(result.Color)),
            RoundResultKind.Draw         => ('D', result.Opponent, ColorLetter(result.Color)),
            RoundResultKind.FullPointBye => ('B', 0, ' '),
            RoundResultKind.HalfPointBye => ('H', 0, ' '),
            RoundResultKind.ZeroPointBye => ('U', 0, ' '),
            RoundResultKind.None         => ('U', 0, ' '),
            _                            => ('U', 0, ' '),
        };

        var opp = opponent.ToString(CultureInfo.InvariantCulture);
        var encoded = colorLetter == ' '
            ? $"{letter}{opp}"
            : $"{letter}{opp}{colorLetter}";
        return encoded.PadRight(Width);
    }

    /// <summary>"U0     " — section played fewer rounds than the file's column count.</summary>
    public static string Unplayed { get; } = "U0".PadRight(Width);

    private static char ColorLetter(PlayerColor c) => c switch
    {
        PlayerColor.White => 'W',
        PlayerColor.Black => 'B',
        _                 => ' ',
    };
}
