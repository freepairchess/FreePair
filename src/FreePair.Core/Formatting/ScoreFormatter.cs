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
        return score.ToString("0.0", CultureInfo.InvariantCulture);
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
