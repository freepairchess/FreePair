using System;
using System.Globalization;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Formatting;

/// <summary>
/// Default <see cref="IScoreFormatter"/> implementation. ASCII mode renders
/// fractional halves as <c>1/2</c> / <c>1 1/2</c>; Unicode mode uses the
/// <c>½</c> glyph.
/// </summary>
public class ScoreFormatter : IScoreFormatter
{
    /// <inheritdoc />
    public bool UseAsciiOnly { get; set; } = true;

    /// <inheritdoc />
    public string Score(decimal score)
    {
        // Integer values render without fractional suffix in both modes.
        if (score == Math.Floor(score))
        {
            return ((long)score).ToString(CultureInfo.InvariantCulture);
        }

        var whole = (long)Math.Floor(score);
        var fraction = score - whole;

        if (fraction == 0.5m)
        {
            if (UseAsciiOnly)
            {
                return whole == 0
                    ? "1/2"
                    : $"{whole} 1/2";
            }

            return whole == 0
                ? "\u00BD"                       // ½
                : $"{whole}\u00BD";
        }

        // Non-half fractions (rare; fall back to invariant decimal).
        return score.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public string PairingResult(PairingResult result) => result switch
    {
        Tournaments.PairingResult.WhiteWins => "1-0",
        Tournaments.PairingResult.BlackWins => "0-1",
        Tournaments.PairingResult.Draw      => UseAsciiOnly ? "1/2-1/2" : "\u00BD-\u00BD",
        _ => "-",
    };
}
