using System.IO;
using System.Linq;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Trf;
using FreePair.Core.Uscf.Trf;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Uscf;

/// <summary>
/// <see cref="TrfReader"/> must be able to consume what
/// <see cref="TrfWriter"/> emits — that's the whole point of using BBP-
/// compatible TRF as the lingua franca between FreePair and the USCF
/// engine. These tests exercise both a hand-rolled minimal document and
/// a real round-trip through <see cref="TrfWriter"/>.
/// </summary>
public class TrfReaderTests
{
    [Fact]
    public void Parse_reads_header_tags()
    {
        const string trf = """
            012 Spring Open - Open
            042 2026/04/01
            052 2026/04/03
            062 0
            092 Individual: Swiss-System
            122 G/60+5
            XXR 5
            XXC white1
            """;

        var doc = TrfReader.Parse(trf);

        Assert.Equal("Spring Open - Open", doc.TournamentName);
        Assert.Equal("2026/04/01", doc.StartDate);
        Assert.Equal("2026/04/03", doc.EndDate);
        Assert.Equal(5, doc.TotalRounds);
        Assert.Equal('w', doc.InitialColor);
        Assert.Empty(doc.Players);
    }

    [Fact]
    public void Parse_reads_initial_color_black()
    {
        const string trf = "XXR 4\nXXC black1\n";
        var doc = TrfReader.Parse(trf);
        Assert.Equal('b', doc.InitialColor);
    }

    [Fact]
    public void Parse_round_trips_TrfWriter_player_lines()
    {
        // Build a tiny in-memory tournament, write it through the
        // production TrfWriter, then parse it back with TrfReader and
        // assert all the player fields survive.
        var section = new Section(
            Name: "Open",
            Title: null,
            Kind: SectionKind.Swiss,
            TimeControl: "G/60+5",
            RoundsPaired: 0,
            RoundsPlayed: 0,
            FinalRound: 5,
            FirstBoard: 1,
            Players:
            [
                new Player(1, "Alice Smith", "12345678", 1800, null, null, null, null, null,
                    System.Array.Empty<int>(), System.Array.Empty<RoundResult>()),
                new Player(2, "Bob Jones", "87654321", 1600, null, null, null, null, null,
                    System.Array.Empty<int>(), System.Array.Empty<RoundResult>()),
                new Player(3, "Charlie Lee", null, 1400, null, null, null, null, null,
                    System.Array.Empty<int>(), System.Array.Empty<RoundResult>()),
            ],
            Teams: System.Array.Empty<Team>(),
            Rounds: System.Array.Empty<Round>(),
            Prizes: Prizes.Empty);

        var tournament = new Tournament(
            Title: "Spring Open",
            StartDate: new System.DateOnly(2026, 4, 1),
            EndDate: new System.DateOnly(2026, 4, 3),
            TimeControl: "G/60+5",
            NachEventId: null,
            Sections: [section]);

        var trfText = TrfWriter.Write(tournament, section, InitialColor.White);
        var doc = TrfReader.Parse(trfText);

        Assert.Equal(3, doc.Players.Count);

        var alice = doc.Players.Single(p => p.PairNumber == 1);
        Assert.Equal("Alice Smith", alice.Name);
        Assert.Equal(1800, alice.Rating);
        Assert.Equal("12345678", alice.Id);
        Assert.Empty(alice.Rounds);

        var charlie = doc.Players.Single(p => p.PairNumber == 3);
        Assert.Equal("Charlie Lee", charlie.Name);
        Assert.Equal(1400, charlie.Rating);
        Assert.Equal(string.Empty, charlie.Id);

        Assert.Equal('w', doc.InitialColor);
    }

    [Fact]
    public void Parse_skips_malformed_player_lines()
    {
        const string trf = """
            XXR 1
            XXC white1
            001
            001 abc not a number
            """;

        var doc = TrfReader.Parse(trf);
        Assert.Empty(doc.Players);
    }

    [Fact]
    public void TryParsePlayer_handles_minimum_columns()
    {
        // Just enough columns: pair number at 5-8.
        var line = new string(' ', 90);
        var chars = line.ToCharArray();
        chars[0] = '0'; chars[1] = '0'; chars[2] = '1';
        // pair number 7 in cols 5-8
        chars[7] = '7';

        Assert.True(TrfReader.TryParsePlayer(new string(chars), out var player));
        Assert.Equal(7, player.PairNumber);
        Assert.Equal(0, player.Rating);
        Assert.Empty(player.Rounds);
    }
}
