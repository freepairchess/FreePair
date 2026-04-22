using FreePair.Core.Bbp;

namespace FreePair.Core.Tests.Bbp;

public class BbpPairingsParserTests
{
    [Fact]
    public void Parse_with_count_header_returns_pairings()
    {
        const string text = """
            3
            1 4
            2 5
            3 6
            """;

        var result = BbpPairingsParser.Parse(text);

        Assert.Equal(3, result.Pairings.Count);
        Assert.Empty(result.ByePlayerPairs);
        Assert.Equal(new BbpPairing(1, 4), result.Pairings[0]);
        Assert.Equal(new BbpPairing(3, 6), result.Pairings[2]);
    }

    [Fact]
    public void Parse_without_count_header_still_works()
    {
        const string text = """
            1 4
            2 5
            """;

        var result = BbpPairingsParser.Parse(text);

        Assert.Equal(2, result.Pairings.Count);
        Assert.Equal(new BbpPairing(1, 4), result.Pairings[0]);
    }

    [Fact]
    public void Parse_reads_bye_when_black_is_zero()
    {
        const string text = """
            3
            1 4
            2 5
            3 0
            """;

        var result = BbpPairingsParser.Parse(text);

        Assert.Equal(2, result.Pairings.Count);
        Assert.Single(result.ByePlayerPairs);
        Assert.Equal(3, result.ByePlayerPairs[0]);
    }

    [Fact]
    public void Parse_reads_bye_when_white_is_zero()
    {
        const string text = """
            0 7
            """;

        var result = BbpPairingsParser.Parse(text);

        Assert.Empty(result.Pairings);
        Assert.Equal(new[] { 7 }, result.ByePlayerPairs.ToArray());
    }

    [Fact]
    public void Parse_tolerates_extra_whitespace_and_CRLF()
    {
        const string text = "3\r\n  1\t4\r\n\r\n2  5\r\n3 0\r\n";

        var result = BbpPairingsParser.Parse(text);

        Assert.Equal(2, result.Pairings.Count);
        Assert.Single(result.ByePlayerPairs);
    }

    [Fact]
    public void Parse_ignores_malformed_lines()
    {
        const string text = """
            2
            1 4
            garbage
            2 5
            """;

        var result = BbpPairingsParser.Parse(text);

        Assert.Equal(2, result.Pairings.Count);
    }

    [Fact]
    public void Parse_returns_empty_for_empty_input()
    {
        Assert.Empty(BbpPairingsParser.Parse(string.Empty).Pairings);
        Assert.Empty(BbpPairingsParser.Parse("   \n  ").Pairings);
    }
}
