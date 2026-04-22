using FreePair.Core.Tournaments;

namespace FreePair.Core.Formatting;

/// <summary>
/// Formats decimal scores and pairing results for display, respecting the
/// application-wide ASCII / Unicode preference.
/// </summary>
public interface IScoreFormatter
{
    /// <summary>
    /// When <c>true</c>, outputs use only ASCII characters (e.g. <c>1/2</c>).
    /// When <c>false</c>, Unicode glyphs are used (e.g. <c>½</c>).
    /// </summary>
    bool UseAsciiOnly { get; set; }

    /// <summary>
    /// Formats a score-like decimal value (score, Solkoff, Mod.Med, etc.).
    /// </summary>
    string Score(decimal score);

    /// <summary>Formats a pairing result.</summary>
    string PairingResult(PairingResult result);
}
