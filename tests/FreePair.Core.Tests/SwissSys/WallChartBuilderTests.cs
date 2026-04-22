using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.WallCharts;

namespace FreePair.Core.Tests.SwissSys;

public class WallChartBuilderTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Theory]
    [InlineData(RoundResultKind.Win,          9,  PlayerColor.Black, "W9B")]
    [InlineData(RoundResultKind.Win,          4,  PlayerColor.White, "W4W")]
    [InlineData(RoundResultKind.Loss,         1,  PlayerColor.White, "L1W")]
    [InlineData(RoundResultKind.Loss,         16, PlayerColor.White, "L16W")]
    [InlineData(RoundResultKind.Draw,         16, PlayerColor.White, "D16W")]
    [InlineData(RoundResultKind.Draw,         7,  PlayerColor.Black, "D7B")]
    [InlineData(RoundResultKind.FullPointBye, -1, PlayerColor.None,  "B---")]
    [InlineData(RoundResultKind.HalfPointBye, -1, PlayerColor.None,  "H---")]
    [InlineData(RoundResultKind.None,         0,  PlayerColor.None,  "U---")]
    public void FormatCell_emits_compact_code(
        RoundResultKind kind, int opponent, PlayerColor color, string expected)
    {
        var result = new RoundResult(kind, opponent, color, 0, 0, 0, 0m);

        Assert.Equal(expected, WallChartBuilder.FormatCell(result));
    }

    [Fact]
    public async Task Build_returns_rows_in_pair_number_order()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var rows = WallChartBuilder.Build(openI);

        Assert.Equal(openI.Players.Count, rows.Count);
        Assert.Equal(
            Enumerable.Range(1, openI.Players.Count).ToArray(),
            rows.Select(r => r.PairNumber).ToArray());
    }

    [Fact]
    public async Task Build_emits_one_cell_per_played_round()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");   // 3 rounds
        var u1000 = t.Sections.Single(s => s.Name == "Under_1000"); // 4 rounds

        var openIRows = WallChartBuilder.Build(openI);
        var u1000Rows = WallChartBuilder.Build(u1000);

        Assert.All(openIRows, r => Assert.Equal(3, r.Cells.Count));
        Assert.All(u1000Rows, r => Assert.Equal(4, r.Cells.Count));
    }

    [Fact]
    public async Task Build_produces_expected_codes_for_Open_I_Pair_1()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var row = WallChartBuilder.Build(openI).Single(r => r.PairNumber == 1);

        Assert.Equal("Maokhampio, Lucas", row.Name);
        Assert.Equal(1951, row.Rating);
        Assert.Equal(new[] { "W9B", "W4W", "W5B" }, row.Cells.Select(c => c.Code).ToArray());
        Assert.Equal(3m, row.Score);
    }

    [Fact]
    public async Task Build_produces_expected_codes_for_Open_I_draws()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        // Pair 7 drew every game.
        var row = WallChartBuilder.Build(openI).Single(r => r.PairNumber == 7);

        Assert.Equal("Saripalli, Prahlada", row.Name);
        Assert.Equal(new[] { "D15B", "D6W", "D11B" }, row.Cells.Select(c => c.Code).ToArray());
    }

    [Fact]
    public async Task Build_renders_half_point_bye_as_H_dashes()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");

        var row = WallChartBuilder.Build(openII)
            .Single(r => r.Name == "Dasari, Vasishta Sai");

        Assert.Equal("H---", row.Cells[0].Code);
        Assert.Equal(RoundResultKind.HalfPointBye, row.Cells[0].Kind);
        Assert.Null(row.Cells[0].Opponent);
    }

    [Fact]
    public async Task Build_renders_full_point_bye_as_B_dashes()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");

        // Xie, Ryan (Pair 11) received a full-point bye in round 3.
        var row = WallChartBuilder.Build(openII).Single(r => r.Name == "Xie, Ryan");

        Assert.Equal("B---", row.Cells[2].Code);
        Assert.Equal(RoundResultKind.FullPointBye, row.Cells[2].Kind);
    }

    [Fact]
    public async Task Build_embeds_tiebreaks_and_score_per_row()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var row = WallChartBuilder.Build(openI).Single(r => r.PairNumber == 1);

        Assert.Equal(3m, row.Score);
        Assert.Equal(3.5m, row.Tiebreaks.ModifiedMedian);
        Assert.Equal(4.5m, row.Tiebreaks.Solkoff);
        Assert.Equal(6m, row.Tiebreaks.Cumulative);
        Assert.Equal(10m, row.Tiebreaks.OpponentCumulative);
    }

    [Fact]
    public async Task Build_preserves_player_identity_fields()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");

        var daasAritra = WallChartBuilder.Build(openII)
            .Single(r => r.Name == "Daas, Aritra");

        Assert.Equal("OR", daasAritra.Club);
        Assert.Equal("OR", daasAritra.State);
        Assert.Equal("Daas", daasAritra.Team);
    }

    [Fact]
    public async Task Build_returns_empty_for_section_with_no_players()
    {
        var t = await LoadAsync();
        var u400 = t.Sections.Single(s => s.Name == "Under_400");

        Assert.Empty(WallChartBuilder.Build(u400));
    }

    [Fact]
    public async Task Build_cells_carry_opponent_and_color_metadata()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var row = WallChartBuilder.Build(openI).Single(r => r.PairNumber == 1);

        Assert.Collection(row.Cells,
            c => { Assert.Equal(9, c.Opponent); Assert.Equal(PlayerColor.Black, c.Color); },
            c => { Assert.Equal(4, c.Opponent); Assert.Equal(PlayerColor.White, c.Color); },
            c => { Assert.Equal(5, c.Opponent); Assert.Equal(PlayerColor.Black, c.Color); });
    }
}
