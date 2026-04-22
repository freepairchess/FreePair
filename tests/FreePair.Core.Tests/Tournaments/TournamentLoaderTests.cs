using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

public class TournamentLoaderTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    [Fact]
    public async Task LoadAsync_returns_mapped_tournament()
    {
        var loader = new TournamentLoader();

        var tournament = await loader.LoadAsync(TestPaths.SwissSysSample(SampleFileName));

        Assert.NotNull(tournament);
        Assert.Equal("Chess A2Z April Open 2026", tournament.Title);
        Assert.Equal(5, tournament.Sections.Count);

        // End-to-end sanity: tiebreak-bearing wall-chart data available.
        var openI = tournament.Sections.Single(s => s.Name == "Open I");
        var pair1 = openI.Players.Single(p => p.PairNumber == 1);
        Assert.Equal(3m, pair1.Score);
    }

    [Fact]
    public async Task LoadAsync_throws_when_file_missing()
    {
        var loader = new TournamentLoader();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => loader.LoadAsync(Path.Combine(Path.GetTempPath(), "no-such-tournament.sjson")));
    }
}
