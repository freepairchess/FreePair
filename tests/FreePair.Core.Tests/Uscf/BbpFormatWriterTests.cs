using FreePair.Core.Uscf;

namespace FreePair.Core.Tests.Uscf;

public class BbpFormatWriterTests
{
    [Fact]
    public void Write_emits_count_followed_by_pair_lines()
    {
        var result = new UscfPairingResult(
            Pairings:
            [
                new UscfPairing(WhitePair: 1, BlackPair: 4, Board: 1),
                new UscfPairing(WhitePair: 5, BlackPair: 2, Board: 2),
                new UscfPairing(WhitePair: 3, BlackPair: 6, Board: 3),
            ],
            ByePair: null);

        var text = BbpFormatWriter.Write(result);

        Assert.Equal(
            "3\n1 4\n5 2\n3 6\n",
            text);
    }

    [Fact]
    public void Write_appends_bye_line_with_zero_for_odd_field()
    {
        var result = new UscfPairingResult(
            Pairings:
            [
                new UscfPairing(1, 4, 1),
                new UscfPairing(2, 5, 2),
            ],
            ByePair: 6);

        var text = BbpFormatWriter.Write(result);

        Assert.Equal(
            "3\n1 4\n2 5\n6 0\n",
            text);
    }

    [Fact]
    public void Round_trips_through_BbpPairingsParser()
    {
        // Whatever we emit, the existing FreePair-side parser must read it
        // back identically — that's the contract that lets TDs swap engine
        // binaries with no other configuration changes.
        var result = new UscfPairingResult(
            Pairings:
            [
                new UscfPairing(2, 7, 1),
                new UscfPairing(8, 3, 2),
                new UscfPairing(4, 9, 3),
            ],
            ByePair: 1);

        var text = BbpFormatWriter.Write(result);
        var parsed = FreePair.Core.Bbp.BbpPairingsParser.Parse(text);

        Assert.Equal(3, parsed.Pairings.Count);
        Assert.Equal(new FreePair.Core.Bbp.BbpPairing(2, 7), parsed.Pairings[0]);
        Assert.Equal(new FreePair.Core.Bbp.BbpPairing(8, 3), parsed.Pairings[1]);
        Assert.Equal(new FreePair.Core.Bbp.BbpPairing(4, 9), parsed.Pairings[2]);
        Assert.Single(parsed.ByePlayerPairs);
        Assert.Equal(1, parsed.ByePlayerPairs[0]);
    }

    [Fact]
    public void Empty_result_emits_zero_count_only()
    {
        var result = new UscfPairingResult(System.Array.Empty<UscfPairing>(), null);
        Assert.Equal("0\n", BbpFormatWriter.Write(result));
    }
}
