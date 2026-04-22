using FreePair.Core.SwissSys;

namespace FreePair.Core.Tests.SwissSys;

public class RoundResultTokenTests
{
    [Theory]
    [InlineData(RoundResultKind.Win,          9, PlayerColor.Black, 1,  0, 0, "0",   "+;9;B;1;0;0;0")]
    [InlineData(RoundResultKind.Loss,         4, PlayerColor.White, 2,  0, 0, "0",   "-;4;W;2;0;0;0")]
    [InlineData(RoundResultKind.Draw,         16, PlayerColor.White, 3, 0, 0, "0",   "=;16;W;3;0;0;0")]
    [InlineData(RoundResultKind.FullPointBye, -1, PlayerColor.None,  0, 0, 0, "0",   "B;-1;-;0;0;0;0")]
    [InlineData(RoundResultKind.HalfPointBye, -1, PlayerColor.None,  0, 0, 0, "0.5", "H;-1;-;0;0;0;0.5")]
    [InlineData(RoundResultKind.None,          0, PlayerColor.None,  0, 0, 0, "0",   "~;0;-;0;0;0;0")]
    [InlineData(RoundResultKind.None,          7, PlayerColor.Black, 2, 0, 0, "0",   "~;7;B;2;0;0;0")]
    public void ToSwissSysToken_renders_canonical_format(
        RoundResultKind kind, int opponent, PlayerColor color, int board,
        int logic1, int logic2, string gamePointsStr, string expected)
    {
        var gp = decimal.Parse(gamePointsStr, System.Globalization.CultureInfo.InvariantCulture);
        var r = new RoundResult(kind, opponent, color, board, logic1, logic2, gp);

        Assert.Equal(expected, r.ToSwissSysToken());
    }

    [Theory]
    [InlineData("+;9;B;1;9;9;0")]
    [InlineData("-;4;W;2;4;4;0")]
    [InlineData("=;16;W;3;6;16;0")]
    [InlineData("B;-1;-;0;-1;-1;0")]
    [InlineData("H;-1;-;0;-1;-1;0")]
    [InlineData("~;0;-;0;0;0;0")]
    public void ToSwissSysToken_roundtrips_parsed_tokens(string original)
    {
        var parsed = RoundResult.Parse(original);
        var written = parsed.ToSwissSysToken();
        var reparsed = RoundResult.Parse(written);

        Assert.Equal(parsed.Kind,       reparsed.Kind);
        Assert.Equal(parsed.Opponent,   reparsed.Opponent);
        Assert.Equal(parsed.Color,      reparsed.Color);
        Assert.Equal(parsed.Board,      reparsed.Board);
        Assert.Equal(parsed.GamePoints, reparsed.GamePoints);
    }
}
