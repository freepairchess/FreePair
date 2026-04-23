using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Bbp;

/// <summary>
/// Argument-building tests for <see cref="BbpPairingEngine"/>. We drive
/// the static <c>BuildArguments</c> helper rather than the async
/// <c>GenerateNextRoundAsync</c> so the assertions run without touching
/// a real bbpPairings subprocess.
/// </summary>
public class BbpPairingEngineArgsTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Section> LoadOpenISectionAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        var t = SwissSysMapper.Map(raw);
        return t.Sections.Single(s => s.Name == "Open I");
    }

    [Fact]
    public async Task BuildArguments_emits_dutch_only_when_acceleration_is_off()
    {
        var section = await LoadOpenISectionAsync();
        Assert.False(section.UseAcceleration); // default for the sample

        var args = BbpPairingEngine.BuildArguments(section, "in.trf", "out.txt");

        Assert.Equal(new[] { "--dutch", "in.trf", "-p", "out.txt" }, args);
        Assert.DoesNotContain("--baku", args);
    }

    [Fact]
    public async Task BuildArguments_adds_baku_when_UseAcceleration_is_true()
    {
        var section = await LoadOpenISectionAsync() with { UseAcceleration = true };

        var args = BbpPairingEngine.BuildArguments(section, "in.trf", "out.txt");

        Assert.Equal(new[] { "--dutch", "--baku", "in.trf", "-p", "out.txt" }, args);
    }

    [Fact]
    public async Task Mapper_preserves_Acceleration_from_SwissSys_Acceleration_field()
    {
        // The April sample has Acceleration=0 for every section (off).
        var section = await LoadOpenISectionAsync();
        Assert.False(section.UseAcceleration);
    }

    [Fact]
    public async Task Mapper_maps_Coin_toss_to_InitialColor()
    {
        // The sample's Coin toss field is 0 (white) — confirm pass-through.
        var section = await LoadOpenISectionAsync();
        Assert.Equal(InitialColor.White, section.InitialColor);
    }

    [Fact]
    public void MapCoinToss_0_is_White_and_1_is_Black()
    {
        Assert.Equal(InitialColor.White, SwissSysMapper.MapCoinToss(0));
        Assert.Equal(InitialColor.Black, SwissSysMapper.MapCoinToss(1));
        // Unknown values fall back to bbpPairings' own default.
        Assert.Equal(InitialColor.White, SwissSysMapper.MapCoinToss(99));
    }
}
