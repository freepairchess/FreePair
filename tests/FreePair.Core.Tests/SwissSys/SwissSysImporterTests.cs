using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tests.SwissSys;

public class SwissSysImporterTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static string SamplePath => TestPaths.SwissSysSample(SampleFileName);

    [Fact]
    public void Sample_file_is_available_to_tests()
    {
        Assert.True(File.Exists(SamplePath),
            $"Expected test asset at: {SamplePath}");
    }

    [Fact]
    public async Task ImportAsync_parses_overview()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);

        Assert.NotNull(doc.Overview);
        Assert.Equal("SwissSys", doc.Overview!.Program);
        Assert.Equal("Chess A2Z April Open 2026", doc.Overview.TournamentTitle);
        Assert.Equal("G/45;d5", doc.Overview.TournamentTimeControls);
        Assert.Equal("2026-04-04", doc.Overview.StartingDate);
        Assert.Equal("2026-04-04", doc.Overview.EndingDate);
        Assert.False(string.IsNullOrWhiteSpace(doc.Overview.NachEventId));
    }

    [Fact]
    public async Task ImportAsync_parses_all_five_sections_in_order()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);

        Assert.Equal(5, doc.Sections.Count);
        Assert.Equal(
            new[] { "Open I", "Open II", "Under_1000", "Under_700", "Under_400" },
            doc.Sections.Select(s => s.SectionName).ToArray());
    }

    [Fact]
    public async Task ImportAsync_reads_open_I_player_counts()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);
        var openI = doc.Sections.Single(s => s.SectionName == "Open I");

        Assert.Equal(16, openI.NumberOfPlayers);
        Assert.Equal(16, openI.Players.Count);
        Assert.Equal(3, openI.RoundsPaired);
        Assert.Equal(3, openI.RoundsPlayed);
        Assert.Empty(openI.Teams);
    }

    [Fact]
    public async Task ImportAsync_reads_player_round_results_as_raw_strings()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);
        var openI = doc.Sections.Single(s => s.SectionName == "Open I");
        var pair1 = openI.Players.Single(p => p.PairNumber == 1);

        Assert.Equal("Maokhampio, Lucas", pair1.Name);
        Assert.Equal("31368597", pair1.Id);
        Assert.Equal(1951, pair1.Rating);
        Assert.Equal(3, pair1.Results.Count);
        Assert.Equal("+;9;B;1;9;9;0", pair1.Results[0]);
    }

    [Fact]
    public async Task ImportAsync_roundtrips_parsed_player_history()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);
        var openI = doc.Sections.Single(s => s.SectionName == "Open I");
        var pair1 = openI.Players.Single(p => p.PairNumber == 1);

        var parsed = pair1.Results.Select(RoundResult.Parse).ToArray();

        Assert.All(parsed, r => Assert.Equal(RoundResultKind.Win, r.Kind));
        Assert.Equal(new[] { 9, 4, 5 }, parsed.Select(r => r.Opponent).ToArray());
        Assert.Equal(
            new[] { PlayerColor.Black, PlayerColor.White, PlayerColor.Black },
            parsed.Select(r => r.Color).ToArray());
        Assert.Equal(3m, parsed.Sum(r => r.Score));
    }

    [Fact]
    public async Task ImportAsync_reads_team_section()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);
        var openII = doc.Sections.Single(s => s.SectionName == "Open II");

        Assert.Equal(2, openII.NumberOfTeams);
        Assert.Equal(2, openII.Teams.Count);
        Assert.Contains(openII.Teams, t => t.FullName == "Daas" && t.TeamCode == "DAAS");

        // Players carry a Team tag in team sections.
        Assert.Contains(openII.Players, p => p.Team == "Daas" && p.Name == "Daas, Aritra");
        Assert.Contains(openII.Players, p => p.Team == "Daas" && p.Name == "Daas, Adrito");
    }

    [Fact]
    public async Task ImportAsync_reads_reserved_byes_and_optional_fields()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);
        var openII = doc.Sections.Single(s => s.SectionName == "Open II");
        var vasishta = openII.Players.Single(p => p.Name == "Dasari, Vasishta Sai");

        Assert.Equal("1 ", vasishta.ReservedByes);
        Assert.Equal(992, vasishta.Rating2);
        Assert.Equal("10/31/2027", vasishta.MembershipExpiration);

        var round1 = RoundResult.Parse(vasishta.Results[0]);
        Assert.Equal(RoundResultKind.HalfPointBye, round1.Kind);
    }

    [Fact]
    public async Task ImportAsync_handles_uninitialized_result_tokens()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);

        // Under_400 has 0 rounds played.
        var u400 = doc.Sections.Single(s => s.SectionName == "Under_400");
        Assert.Equal(0, u400.RoundsPlayed);

        // Somewhere in the file, at least one team result is the NUL-prefixed
        // "uninitialized" token "\u0000;0;-;0;0;0;0" — verify the parser
        // accepts it without throwing.
        var u700 = doc.Sections.Single(s => s.SectionName == "Under_700");
        var tokens = u700.Teams.SelectMany(t => t.Results).ToArray();
        Assert.NotEmpty(tokens);
        foreach (var token in tokens)
        {
            var parsed = RoundResult.Parse(token);
            Assert.Equal(RoundResultKind.None, parsed.Kind);
        }
    }

    [Fact]
    public async Task ImportAsync_reads_place_prizes()
    {
        var importer = new SwissSysImporter();

        var doc = await importer.ImportAsync(SamplePath);
        var openI = doc.Sections.Single(s => s.SectionName == "Open I");

        Assert.NotNull(openI.Prizes);
        Assert.Equal(3, openI.Prizes!.PlacePrizes.Count);
        Assert.Equal(50m, openI.Prizes.PlacePrizes[0].Value);
        Assert.Equal(30m, openI.Prizes.PlacePrizes[1].Value);
        Assert.Equal(20m, openI.Prizes.PlacePrizes[2].Value);
    }

    [Fact]
    public async Task ImportAsync_throws_for_missing_file()
    {
        var importer = new SwissSysImporter();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => importer.ImportAsync(Path.Combine(Path.GetTempPath(),
                "definitely-does-not-exist.sjson")));
    }

    [Fact]
    public async Task ImportAsync_throws_for_invalid_json()
    {
        var importer = new SwissSysImporter();
        using var stream = new MemoryStream(
            System.Text.Encoding.UTF8.GetBytes("{ not valid json"));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => importer.ImportAsync(stream));
    }
}
