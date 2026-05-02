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

    [Fact]
    public void Parse_extracts_upcoming_round_half_point_bye_into_RequestedByes()
    {
        // Four-player section, round 1 paired 1v2 and 3v4 (so
        // playedRounds = 1). Player 3 has a pre-flagged half-point
        // bye for the upcoming round 2 (cell at index 1 is "0000 - H").
        // The other three played round 1 normally and have no
        // upcoming-round cell, so they're absent from RequestedByes.
        var p1 = MakePlayerLine(pair: 1, name: "Alpha", rating: 2000, points: 1m,
            new[] { Cell(opp: 2, color: 'w', result: '1') });
        var p2 = MakePlayerLine(pair: 2, name: "Beta",  rating: 1900, points: 0m,
            new[] { Cell(opp: 1, color: 'b', result: '0') });
        var p3 = MakePlayerLine(pair: 3, name: "Gamma", rating: 1800, points: 1.5m,
            new[]
            {
                Cell(opp: 4, color: 'w', result: '1'),  // round 1: actual game
                Cell(opp: 0, color: '-', result: 'H'),  // round 2: pre-flagged half-bye
            });
        var p4 = MakePlayerLine(pair: 4, name: "Delta", rating: 1700, points: 0m,
            new[] { Cell(opp: 3, color: 'b', result: '0') });

        var trf = $"XXR 5\n{p1}\n{p2}\n{p3}\n{p4}\n";

        var doc = TrfReader.Parse(trf);

        Assert.NotNull(doc.RequestedByes);
        Assert.Single(doc.RequestedByes!);
        Assert.Equal('H', doc.RequestedByes![3]);
        // Players 1, 2, 4 are NOT in the dict — none has an upcoming-
        // round H cell.
        Assert.False(doc.RequestedByes!.ContainsKey(1));
        Assert.False(doc.RequestedByes!.ContainsKey(2));
        Assert.False(doc.RequestedByes!.ContainsKey(4));
    }

    [Fact]
    public void Parse_ignores_past_round_half_point_byes()
    {
        // Two players: player 1 played player 2 in round 2 (so
        // playedRounds = 2). Player 1 has a past H cell at round 1
        // (already-honoured bye, not an upcoming request).
        var p1 = MakePlayerLine(pair: 1, name: "Alpha", rating: 2000, points: 1.5m,
            new[]
            {
                Cell(opp: 0, color: '-', result: 'H'),  // round 1: PAST half-bye
                Cell(opp: 2, color: 'w', result: '1'),  // round 2: actual game
            });
        var p2 = MakePlayerLine(pair: 2, name: "Beta",  rating: 1900, points: 0m,
            new[]
            {
                Cell(opp: 0, color: '-', result: '-'),  // round 1: didn't play
                Cell(opp: 1, color: 'b', result: '0'),  // round 2: actual game
            });

        var trf = $"XXR 5\n{p1}\n{p2}\n";

        var doc = TrfReader.Parse(trf);

        Assert.Null(doc.RequestedByes);
    }

    /// <summary>
    /// Render a TRF 001-line with the FIDE column layout TrfWriter
    /// emits. Pads each round cell to the canonical 10-char width.
    /// </summary>
    private static string MakePlayerLine(
        int pair, string name, int rating, decimal points, IReadOnlyList<string> roundCells)
    {
        // Layout: cols 1-3 "001", 5-8 pair, 15-47 name, 49-52 rating,
        // 81-84 points, 91+ rounds.
        var sb = new System.Text.StringBuilder(new string(' ', 90));
        sb[0] = '0'; sb[1] = '0'; sb[2] = '1';
        var pairStr = pair.ToString().PadLeft(4);
        for (var i = 0; i < 4; i++) sb[4 + i] = pairStr[i];
        var nameStr = name.PadRight(33).Substring(0, 33);
        for (var i = 0; i < 33; i++) sb[14 + i] = nameStr[i];
        var ratingStr = rating.ToString().PadLeft(4);
        for (var i = 0; i < 4; i++) sb[48 + i] = ratingStr[i];
        var pointsStr = points.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).PadLeft(4);
        for (var i = 0; i < 4; i++) sb[80 + i] = pointsStr[i];

        var line = sb.ToString();
        foreach (var cell in roundCells) line += cell;
        return line;
    }

    /// <summary>
    /// Render a single round cell in the FIDE 10-char layout: " OOOO C R "
    /// (1 space, 4-digit opponent, 1 space, colour, 1 space, result, 1 space).
    /// </summary>
    private static string Cell(int opp, char color, char result)
    {
        var oppStr = opp == 0 ? "    " : opp.ToString().PadLeft(4);
        return $" {oppStr} {color} {result} ";
    }
}
