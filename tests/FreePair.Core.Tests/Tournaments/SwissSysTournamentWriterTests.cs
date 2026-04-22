using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

public class SwissSysTournamentWriterTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<(Tournament Tournament, string TempPath)> LoadIntoTempAsync()
    {
        var source = TestPaths.SwissSysSample(SampleFileName);
        var tempPath = Path.Combine(Path.GetTempPath(), $"freepair-writer-{System.Guid.NewGuid():N}.sjson");
        File.Copy(source, tempPath, overwrite: true);

        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(tempPath);
        return (SwissSysMapper.Map(raw), tempPath);
    }

    [Fact]
    public async Task SaveAsync_roundtrips_result_changes()
    {
        var (tournament, tempPath) = await LoadIntoTempAsync();
        try
        {
            var openI = tournament.Sections.Single(s => s.Name == "Open I");
            var r1b1 = openI.Rounds[0].Pairings.Single(p => p.Board == 1);

            var mutated = TournamentMutations.SetPairingResult(
                tournament, "Open I", 1, r1b1.WhitePair, r1b1.BlackPair, PairingResult.Draw);

            var writer = new SwissSysTournamentWriter();
            await writer.SaveAsync(tempPath, mutated);

            // Re-import from disk and check the mutation survived.
            var raw = await new SwissSysImporter().ImportAsync(tempPath);
            var reloaded = SwissSysMapper.Map(raw);
            var openIReloaded = reloaded.Sections.Single(s => s.Name == "Open I");
            var board1 = openIReloaded.Rounds[0].Pairings.Single(p => p.Board == 1);

            Assert.Equal(PairingResult.Draw, board1.Result);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    [Fact]
    public async Task SaveAsync_roundtrips_appended_round_with_unplayed_pairings()
    {
        var (tournament, tempPath) = await LoadIntoTempAsync();
        try
        {
            var openI = tournament.Sections.Single(s => s.Name == "Open I");
            var pairings = new[]
            {
                new FreePair.Core.Bbp.BbpPairing(1, 2),
                new FreePair.Core.Bbp.BbpPairing(3, 4),
                new FreePair.Core.Bbp.BbpPairing(5, 6),
                new FreePair.Core.Bbp.BbpPairing(7, 8),
                new FreePair.Core.Bbp.BbpPairing(9, 10),
                new FreePair.Core.Bbp.BbpPairing(11, 12),
                new FreePair.Core.Bbp.BbpPairing(13, 14),
                new FreePair.Core.Bbp.BbpPairing(15, 16),
            };

            var appended = TournamentMutations.AppendRound(
                tournament, "Open I",
                new FreePair.Core.Bbp.BbpPairingResult(pairings, System.Array.Empty<int>()));

            await new SwissSysTournamentWriter().SaveAsync(tempPath, appended);

            // Re-import and verify round 4 exists with unplayed pairings.
            var raw = await new SwissSysImporter().ImportAsync(tempPath);
            var reloaded = SwissSysMapper.Map(raw);
            var openIReloaded = reloaded.Sections.Single(s => s.Name == "Open I");

            Assert.Equal(4, openIReloaded.Rounds.Count);
            Assert.Equal(4, openIReloaded.RoundsPaired);
            Assert.Equal(3, openIReloaded.RoundsPlayed);
            Assert.Equal(pairings.Length, openIReloaded.Rounds[3].Pairings.Count);
            Assert.All(openIReloaded.Rounds[3].Pairings,
                p => Assert.Equal(PairingResult.Unplayed, p.Result));
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    [Fact]
    public async Task SaveAsync_preserves_unmodelled_fields()
    {
        var (tournament, tempPath) = await LoadIntoTempAsync();
        try
        {
            var originalJson = await File.ReadAllTextAsync(tempPath);
            var originalRoot = JsonNode.Parse(originalJson)!;

            // Save unchanged tournament — file should still contain every
            // unmodelled top-level key.
            await new SwissSysTournamentWriter().SaveAsync(tempPath, tournament);

            var afterJson = await File.ReadAllTextAsync(tempPath);
            var afterRoot = JsonNode.Parse(afterJson)!;

            // Overview is a field we don't touch — it must survive verbatim.
            Assert.NotNull(afterRoot["Overview"]);
            Assert.NotNull(afterRoot["Sections"]);

            var openIBefore = (JsonObject)originalRoot["Sections"]!.AsArray()[0]!;
            var openIAfter  = (JsonObject)afterRoot["Sections"]!.AsArray()[0]!;

            // Section-level fields we don't manage must be preserved.
            foreach (var key in new[] { "Type", "Section title", "Section time control",
                                        "Rating to use", "Engine", "Final round",
                                        "Prizes", "Teams" })
            {
                Assert.Equal(
                    openIBefore[key]?.ToJsonString(),
                    openIAfter[key]?.ToJsonString());
            }
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    [Fact]
    public async Task SaveAsync_is_atomic_even_if_target_is_overwritten_repeatedly()
    {
        var (tournament, tempPath) = await LoadIntoTempAsync();
        try
        {
            var writer = new SwissSysTournamentWriter();

            // Multiple consecutive saves should all succeed with no orphan
            // temp files left behind.
            await writer.SaveAsync(tempPath, tournament);
            await writer.SaveAsync(tempPath, tournament);
            await writer.SaveAsync(tempPath, tournament);

            var directory = Path.GetDirectoryName(tempPath)!;
            var fileName = Path.GetFileName(tempPath);
            var leftovers = Directory
                .EnumerateFiles(directory, $".{fileName}.*.tmp")
                .ToArray();

            Assert.Empty(leftovers);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }
}
