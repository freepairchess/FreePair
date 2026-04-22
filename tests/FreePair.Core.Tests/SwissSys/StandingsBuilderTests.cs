using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Standings;

namespace FreePair.Core.Tests.SwissSys;

public class StandingsBuilderTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task Build_Open_I_matches_oracle_order()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var rows = StandingsBuilder.Build(openI);

        // Oracle from companion .json: pair numbers in finishing order.
        Assert.Equal(
            new[] { 1, 16, 5, 2, 8, 3, 6, 7, 11, 9, 4, 15, 10, 12, 13, 14 },
            rows.Select(r => r.PairNumber).ToArray());
    }

    [Fact]
    public async Task Build_Open_I_places_handle_ties()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var rows = StandingsBuilder.Build(openI);

        Assert.Equal(
            new[] { "1", "2", "3-5", "3-5", "3-5", "6-10", "6-10", "6-10", "6-10", "6-10",
                    "11-15", "11-15", "11-15", "11-15", "11-15", "16" },
            rows.Select(r => r.Place).ToArray());
    }

    [Fact]
    public async Task Build_Open_II_matches_oracle_order()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");

        var rows = StandingsBuilder.Build(openII);

        Assert.Equal(
            new[] { 1, 16, 13, 4, 2, 12, 15, 7, 14, 17, 8, 6, 10, 3, 9, 11, 18, 5 },
            rows.Select(r => r.PairNumber).ToArray());
    }

    [Fact]
    public async Task Build_Under_1000_matches_oracle_order()
    {
        var t = await LoadAsync();
        var u1000 = t.Sections.Single(s => s.Name == "Under_1000");

        var rows = StandingsBuilder.Build(u1000);

        Assert.Equal(
            new[] { 1, 5, 4, 7, 8, 10, 9, 3, 12, 11, 2, 14, 13, 6 },
            rows.Select(r => r.PairNumber).ToArray());
    }

    [Fact]
    public async Task Build_Under_700_full_oracle_order()
    {
        var t = await LoadAsync();
        var u700 = t.Sections.Single(s => s.Name == "Under_700");

        var rows = StandingsBuilder.Build(u700);

        Assert.Equal(
            new[] { 1, 6, 8, 2, 4, 15, 11, 7, 5, 13, 12, 9, 10, 14, 3, 16 },
            rows.Select(r => r.PairNumber).ToArray());
    }

    [Fact]
    public async Task Build_Open_II_place_labels_match_oracle()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");

        var rows = StandingsBuilder.Build(openII);

        Assert.Equal(
            new[] { "1-2", "1-2", "3-4", "3-4", "5-7", "5-7", "5-7",
                    "8-10", "8-10", "8-10", "11-16", "11-16", "11-16",
                    "11-16", "11-16", "11-16", "17", "18" },
            rows.Select(r => r.Place).ToArray());
    }

    [Fact]
    public async Task Build_ranks_are_sequential_and_dense()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var rows = StandingsBuilder.Build(openI);

        Assert.Equal(
            Enumerable.Range(1, openI.Players.Count).ToArray(),
            rows.Select(r => r.Rank).ToArray());
    }

    [Fact]
    public async Task Build_embeds_tiebreaks_per_row()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var rows = StandingsBuilder.Build(openI);
        var first = rows[0];

        Assert.Equal(1, first.PairNumber);
        Assert.Equal(3m, first.Score);
        Assert.Equal(3.5m, first.Tiebreaks.ModifiedMedian);
        Assert.Equal(4.5m, first.Tiebreaks.Solkoff);
    }

    [Fact]
    public async Task Build_returns_empty_for_empty_section()
    {
        var t = await LoadAsync();
        var u400 = t.Sections.Single(s => s.Name == "Under_400");

        Assert.Empty(StandingsBuilder.Build(u400));
    }
}
