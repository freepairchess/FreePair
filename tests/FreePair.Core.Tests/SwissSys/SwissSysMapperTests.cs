using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.SwissSys;

public class SwissSysMapperTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task Map_populates_overview()
    {
        var t = await LoadAsync();

        Assert.Equal("Chess A2Z April Open 2026", t.Title);
        Assert.Equal(new DateOnly(2026, 4, 4), t.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 4), t.EndDate);
        Assert.Equal("G/45;d5", t.TimeControl);
        Assert.False(string.IsNullOrWhiteSpace(t.NachEventId));
    }

    [Fact]
    public async Task Map_includes_all_sections_in_order()
    {
        var t = await LoadAsync();

        Assert.Equal(
            new[] { "Open I", "Open II", "Under_1000", "Under_700", "Under_400" },
            t.Sections.Select(s => s.Name).ToArray());

        Assert.All(t.Sections, s => Assert.Equal(SectionKind.Swiss, s.Kind));
    }

    [Fact]
    public void MapKind_maps_SwissSys_Type_0_to_Swiss_and_1_to_RoundRobin()
    {
        Assert.Equal(SectionKind.Swiss,      SwissSysMapper.MapKind(0));
        Assert.Equal(SectionKind.RoundRobin, SwissSysMapper.MapKind(1));
        Assert.Equal(SectionKind.Unknown,    SwissSysMapper.MapKind(99));
    }

    [Fact]
    public async Task Map_open_I_player_scores_match_expected_standings()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        // Expected top of standings from the companion .json: pair 1 = 3.0.
        var pair1 = openI.Players.Single(p => p.PairNumber == 1);
        Assert.Equal("Maokhampio, Lucas", pair1.Name);
        Assert.Equal(3m, pair1.Score);

        // Pair 7 drew all three games.
        var pair7 = openI.Players.Single(p => p.PairNumber == 7);
        Assert.Equal(1.5m, pair7.Score);

        // Pair 14 lost all three games.
        var pair14 = openI.Players.Single(p => p.PairNumber == 14);
        Assert.Equal(0m, pair14.Score);
    }

    [Fact]
    public async Task Map_reconstructs_rounds_from_player_history()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        Assert.Equal(3, openI.Rounds.Count);

        // Round 1 should have 8 pairings (16 players / 2) and no byes.
        var r1 = openI.Rounds[0];
        Assert.Equal(1, r1.Number);
        Assert.Equal(8, r1.Pairings.Count);
        Assert.Empty(r1.Byes);

        // Board 1 round 1 in Open I: Pair 9 (White) vs Pair 1 (Black), black won.
        var board1 = r1.Pairings.Single(p => p.Board == 1);
        Assert.Equal(9, board1.WhitePair);
        Assert.Equal(1, board1.BlackPair);
        Assert.Equal(PairingResult.BlackWins, board1.Result);
    }

    [Fact]
    public async Task Map_deduplicates_each_pairing_once_per_round()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        foreach (var round in openI.Rounds)
        {
            var unique = round.Pairings
                .Select(p => (System.Math.Min(p.WhitePair, p.BlackPair),
                              System.Math.Max(p.WhitePair, p.BlackPair)))
                .Distinct()
                .Count();

            Assert.Equal(round.Pairings.Count, unique);
        }
    }

    [Fact]
    public async Task Map_classifies_byes_correctly()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");

        // Vasishta requested a half-point bye in round 1.
        var vasishta = openII.Players.Single(p => p.Name == "Dasari, Vasishta Sai");
        Assert.Contains(1, vasishta.RequestedByeRounds);
        Assert.Equal(0.5m, vasishta.History[0].Score);

        // Round 1 of Open II should list him as a half-point bye.
        var r1 = openII.Rounds[0];
        Assert.Contains(r1.Byes, b => b.PlayerPair == vasishta.PairNumber && b.Kind == ByeKind.Half);

        // "Xie, Ryan" received a full-point bye in round 3.
        var ryan = openII.Players.Single(p => p.Name == "Xie, Ryan");
        var r3 = openII.Rounds[2];
        Assert.Contains(r3.Byes, b => b.PlayerPair == ryan.PairNumber && b.Kind == ByeKind.Full);
    }

    [Fact]
    public async Task Map_exposes_teams_only_in_team_sections()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");
        var openII = t.Sections.Single(s => s.Name == "Open II");

        Assert.False(openI.HasTeams);
        Assert.Empty(openI.Teams);

        Assert.True(openII.HasTeams);
        Assert.Equal(2, openII.Teams.Count);
        Assert.Contains(openII.Teams, tm => tm.Name == "Daas" && tm.Code == "DAAS");

        // Players with a Team tag are linked to team records.
        Assert.Contains(openII.Players, p => p.Team == "Daas" && p.Name == "Daas, Aritra");
        Assert.Contains(openII.Players, p => p.Team == "Daas" && p.Name == "Daas, Adrito");
    }

    [Fact]
    public async Task Map_preserves_optional_player_fields()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");
        var vasishta = openII.Players.Single(p => p.Name == "Dasari, Vasishta Sai");

        Assert.Equal(992, vasishta.SecondaryRating);
        Assert.Equal("10/31/2027", vasishta.MembershipExpiration);
    }

    [Fact]
    public void MapPlayer_round_trips_email_and_phone_from_raw()
    {
        // The bundled sample pre-dates the Email/Phone feature, so we
        // exercise the mapper directly against a synthesised RawPlayer
        // to lock down the JSON key mapping.
        var raw = new FreePair.Core.SwissSys.Raw.RawPlayer
        {
            PairNumber = 1,
            Name = "Test Player",
            Rating = 1500,
            Email = "player@example.com",
            Phone = "+1-555-0100",
        };

        var p = SwissSysMapper.MapPlayer(raw);

        Assert.Equal("player@example.com", p.Email);
        Assert.Equal("+1-555-0100",       p.Phone);
    }

    [Fact]
    public async Task Map_preserves_place_prizes()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        Assert.Equal(3, openI.Prizes.Place.Count);
        Assert.Equal(new[] { 50m, 30m, 20m },
            openI.Prizes.Place.Select(p => p.Value).ToArray());
        Assert.Empty(openI.Prizes.Class);
    }

    [Fact]
    public async Task Map_handles_section_with_no_rounds_played()
    {
        var t = await LoadAsync();
        var u400 = t.Sections.Single(s => s.Name == "Under_400");

        Assert.Equal(0, u400.RoundsPlayed);
        Assert.Empty(u400.Rounds);
    }

    [Theory]
    [InlineData(null, new int[0])]
    [InlineData("", new int[0])]
    [InlineData("   ", new int[0])]
    [InlineData("1", new[] { 1 })]
    [InlineData("1 ", new[] { 1 })]
    [InlineData(" 1 3 ", new[] { 1, 3 })]
    [InlineData("3 1", new[] { 1, 3 })]
    [InlineData("1,3;4", new[] { 1, 3, 4 })]
    [InlineData("2 2 2", new[] { 2 })]
    [InlineData("0 -1 5", new[] { 5 })]
    public void ParseReservedByes_interprets_variants(string? input, int[] expected)
    {
        var parsed = SwissSysMapper.ParseReservedByes(input);
        Assert.Equal(expected, parsed.ToArray());
    }

    [Fact]
    public async Task Map_open_I_board1_round2_and_round3_match_known_results()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        // Round 2 board 1: Pair 1 (W) vs Pair 4 (B), white won.
        var r2b1 = openI.Rounds[1].Pairings.Single(p => p.Board == 1);
        Assert.Equal(1, r2b1.WhitePair);
        Assert.Equal(4, r2b1.BlackPair);
        Assert.Equal(PairingResult.WhiteWins, r2b1.Result);

        // Round 2 board 3: Pair 3 (W) vs Pair 16 (B), drawn.
        var r2b3 = openI.Rounds[1].Pairings.Single(p => p.Board == 3);
        Assert.Equal(3, r2b3.WhitePair);
        Assert.Equal(16, r2b3.BlackPair);
        Assert.Equal(PairingResult.Draw, r2b3.Result);

        // Round 3 board 1: Pair 5 (W) vs Pair 1 (B), black won.
        var r3b1 = openI.Rounds[2].Pairings.Single(p => p.Board == 1);
        Assert.Equal(5, r3b1.WhitePair);
        Assert.Equal(1, r3b1.BlackPair);
        Assert.Equal(PairingResult.BlackWins, r3b1.Result);
    }
}
