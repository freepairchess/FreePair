using FreePair.Core.SwissSys;

namespace FreePair.Core.Tests.SwissSys;

public class RoundResultTests
{
    [Theory]
    [InlineData("+;9;B;1;9;9;0", RoundResultKind.Win, 9, PlayerColor.Black, 1)]
    [InlineData("-;4;B;4;4;4;0", RoundResultKind.Loss, 4, PlayerColor.Black, 4)]
    [InlineData("=;16;W;3;6;16;0", RoundResultKind.Draw, 16, PlayerColor.White, 3)]
    [InlineData("H;-1;-;0;-1;-1;0", RoundResultKind.HalfPointBye, -1, PlayerColor.None, 0)]
    [InlineData("B;-1;-;0;-1;-1;0", RoundResultKind.FullPointBye, -1, PlayerColor.None, 0)]
    [InlineData("~;0;-;0;0;0;0", RoundResultKind.None, 0, PlayerColor.None, 0)]
    public void Parse_recognizes_all_observed_prefixes(
        string token,
        RoundResultKind expectedKind,
        int expectedOpponent,
        PlayerColor expectedColor,
        int expectedBoard)
    {
        var result = RoundResult.Parse(token);

        Assert.Equal(expectedKind, result.Kind);
        Assert.Equal(expectedOpponent, result.Opponent);
        Assert.Equal(expectedColor, result.Color);
        Assert.Equal(expectedBoard, result.Board);
    }

    [Fact]
    public void Parse_treats_leading_NUL_as_uninitialized()
    {
        var result = RoundResult.Parse("\u0000;0;-;0;0;0;0");

        Assert.Equal(RoundResultKind.None, result.Kind);
        Assert.True(result.IsUnplayed);
        Assert.False(result.IsBye);
    }

    [Fact]
    public void Parse_preserves_logic_and_game_points()
    {
        var result = RoundResult.Parse("+;12;W;7;3;12;0");

        Assert.Equal(3, result.Logic1);
        Assert.Equal(12, result.Logic2);
        Assert.Equal(0m, result.GamePoints);
    }

    [Theory]
    [InlineData(RoundResultKind.Win, 1)]
    [InlineData(RoundResultKind.FullPointBye, 1)]
    [InlineData(RoundResultKind.Draw, 0.5)]
    [InlineData(RoundResultKind.HalfPointBye, 0.5)]
    [InlineData(RoundResultKind.Loss, 0)]
    [InlineData(RoundResultKind.None, 0)]
    public void Score_matches_USChess_scoring(RoundResultKind kind, decimal expected)
    {
        var result = new RoundResult(kind, 0, PlayerColor.None, 0, 0, 0, 0m);

        Assert.Equal(expected, result.Score);
    }

    [Fact]
    public void TryParse_returns_false_for_unknown_prefix()
    {
        var ok = RoundResult.TryParse("Q;1;W;1;0;0;0", out var result);

        Assert.False(ok);
        Assert.Equal(RoundResult.Empty, result);
    }

    [Fact]
    public void Parse_throws_on_unknown_prefix()
    {
        Assert.Throws<FormatException>(() => RoundResult.Parse("Q;1;W;1;0;0;0"));
    }

    [Fact]
    public void Parse_tolerates_truncated_tokens()
    {
        // If the tail is missing, unspecified fields should fall back to
        // their defaults rather than crashing.
        var result = RoundResult.Parse("+;5;W");

        Assert.Equal(RoundResultKind.Win, result.Kind);
        Assert.Equal(5, result.Opponent);
        Assert.Equal(PlayerColor.White, result.Color);
        Assert.Equal(0, result.Board);
    }

    [Fact]
    public void Empty_is_equivalent_to_default_NUL_token()
    {
        Assert.Equal(RoundResultKind.None, RoundResult.Empty.Kind);
        Assert.Equal(0, RoundResult.Empty.Opponent);
        Assert.Equal(PlayerColor.None, RoundResult.Empty.Color);
    }
}
