using FreePair.Core.Formatting;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Formatting;

public class ScoreFormatterTests
{
    [Theory]
    [InlineData(0.0,  "0")]
    [InlineData(1.0,  "1")]
    [InlineData(3.0,  "3")]
    [InlineData(0.5,  "1/2")]
    [InlineData(1.5,  "1 1/2")]
    [InlineData(2.5,  "2 1/2")]
    [InlineData(3.5,  "3 1/2")]
    [InlineData(29.5, "29 1/2")]
    public void Score_ASCII_mode_uses_slash(decimal input, string expected)
    {
        var f = new ScoreFormatter { UseAsciiOnly = true };
        Assert.Equal(expected, f.Score(input));
    }

    [Theory]
    [InlineData(0.0,  "0")]
    [InlineData(1.0,  "1")]
    [InlineData(0.5,  "\u00BD")]
    [InlineData(1.5,  "1\u00BD")]
    [InlineData(29.5, "29\u00BD")]
    public void Score_Unicode_mode_uses_half_glyph(decimal input, string expected)
    {
        var f = new ScoreFormatter { UseAsciiOnly = false };
        Assert.Equal(expected, f.Score(input));
    }

    [Fact]
    public void Score_non_half_fraction_falls_back_to_decimal()
    {
        var f = new ScoreFormatter { UseAsciiOnly = true };
        Assert.Equal("0.25", f.Score(0.25m));
    }

    [Theory]
    [InlineData(PairingResult.WhiteWins, true,  "1-0")]
    [InlineData(PairingResult.BlackWins, true,  "0-1")]
    [InlineData(PairingResult.Draw,      true,  "1/2-1/2")]
    [InlineData(PairingResult.Unplayed,  true,  "-")]
    [InlineData(PairingResult.Draw,      false, "\u00BD-\u00BD")]
    [InlineData(PairingResult.WhiteWins, false, "1-0")]
    public void PairingResult_respects_mode(PairingResult input, bool ascii, string expected)
    {
        var f = new ScoreFormatter { UseAsciiOnly = ascii };
        Assert.Equal(expected, f.PairingResult(input));
    }
}
