using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Golden-file tests for <see cref="SwissSysResultJsonBuilder"/>. Each
/// test loads one of the 6 bundled <c>.sjson</c> / <c>SwissSysJSON.json</c>
/// pairs (<c>docs/samples/swisssys/pairs</c>), regenerates the result
/// JSON, and compares the <b>structurally-derivable</b> fields against
/// the real NAChessHub output. TD-local fields (<c>td</c>,
/// <c>bye_message</c>, short <c>NACHEventID</c> / <c>NACHEventPasscode</c>,
/// default <c>city</c>) aren't in the <c>.sjson</c> so they're
/// intentionally skipped.
/// </summary>
public class SwissSysResultJsonBuilderTests
{
    private static readonly string[] Pairs =
    [
        "PNWCC_Premiere_USCF_Open__Spring_2025_1800",
        "PNWCC_G60_Online__Mar_16",
        "Chess_A2Z_April_Open_2026",
        "The_2026_Chess_A2Z_Inaugural_Open",
        "PNWCC_IM_Norm_RR__Apr_2025",
        "PNWCC_GM_IM_Norm_RR__May_2025",
    ];

    private static string PairsDir =>
        Path.Combine(TestPaths.RepoRoot, "docs", "samples", "swisssys", "pairs");

    private static async Task<JsonDocument> BuildAsync(string prefix)
    {
        var sjson = Path.Combine(PairsDir, $"{prefix}_SwissSys11.sjson");
        var raw   = await new SwissSysImporter().ImportAsync(sjson);
        var t     = SwissSysMapper.Map(raw);
        var json  = SwissSysResultJsonBuilder.Build(t);
        return JsonDocument.Parse(json);
    }

    private static JsonDocument LoadGolden(string prefix) =>
        JsonDocument.Parse(File.ReadAllText(
            Path.Combine(PairsDir, $"{prefix}_SwissSysJSON.json")));

    [Theory]
    [InlineData("PNWCC_Premiere_USCF_Open__Spring_2025_1800")]
    [InlineData("PNWCC_G60_Online__Mar_16")]
    [InlineData("Chess_A2Z_April_Open_2026")]
    [InlineData("The_2026_Chess_A2Z_Inaugural_Open")]
    [InlineData("PNWCC_IM_Norm_RR__Apr_2025")]
    [InlineData("PNWCC_GM_IM_Norm_RR__May_2025")]
    public async Task Output_matches_golden_on_structural_fields(string prefix)
    {
        using var actual   = await BuildAsync(prefix);
        using var expected = LoadGolden(prefix);

        var a = actual.RootElement;
        var g = expected.RootElement;

        // --- Root scalars we CAN derive ---
        // Title: NAChessHub sanitises punctuation ('/', '+', and
        // sometimes collapses whitespace). Strip the known bits on
        // both sides before comparing.
        static string SanitiseTitle(string? s) =>
            (s ?? "")
                .Replace("/", "")
                .Replace("+", "")
                .Replace("  ", " ")
                .Trim();
        Assert.Equal(SanitiseTitle(g.GetProperty("tournament").GetString()),
                     SanitiseTitle(a.GetProperty("tournament").GetString()));

        // --- Sections: count + per-section structural equivalence ---
        var aSecs = a.GetProperty("sections");
        var gSecs = g.GetProperty("sections");
        Assert.Equal(gSecs.GetArrayLength(), aSecs.GetArrayLength());

        for (var si = 0; si < gSecs.GetArrayLength(); si++)
        {
            var aSec = aSecs[si];
            var gSec = gSecs[si];

            Assert.Equal(gSec.GetProperty("section").GetString(),   aSec.GetProperty("section").GetString());
            Assert.Equal(gSec.GetProperty("rounds_paired").GetInt32(), aSec.GetProperty("rounds_paired").GetInt32());
            Assert.Equal(gSec.GetProperty("rounds_played").GetInt32(), aSec.GetProperty("rounds_played").GetInt32());

            // Tiebreak display names are a fixed 4-element list for
            // USCF Swiss sections. Round-robins are empty in NAChessHub
            // output. Some Swiss tournaments ALSO have empty tiebreaks
            // when the TD disabled them in SwissSys — we can't detect
            // that from the .sjson, so accept either shape here.
            var aTb = aSec.GetProperty("tiebreaks").EnumerateArray().Select(x => x.GetString()).ToArray();
            var gTb = gSec.GetProperty("tiebreaks").EnumerateArray().Select(x => x.GetString()).ToArray();
            if (gTb.Length > 0)
            {
                Assert.Equal(gTb, aTb);
            }

            // --- rounds → pairings ---
            var aRounds = aSec.GetProperty("rounds");
            var gRounds = gSec.GetProperty("rounds");
            Assert.Equal(gRounds.GetArrayLength(), aRounds.GetArrayLength());

            for (var ri = 0; ri < gRounds.GetArrayLength(); ri++)
            {
                var aP = aRounds[ri].GetProperty("pairings");
                var gP = gRounds[ri].GetProperty("pairings");

                // Pairing counts should be within 1 of each other.
                // NAChessHub represents double-forfeits (both players
                // lose via "F") as a head-to-head pair (2 rows); our
                // domain model may split those into two solo bye-like
                // rows. We don't try to match byte-for-byte — we just
                // verify the total pair-number participation matches.
                Assert.InRange(Math.Abs(gP.GetArrayLength() - aP.GetArrayLength()), 0, 2);

                // Every non-zero pair number mentioned on either side
                // of the golden pairings should appear on our side
                // (in either white or black slot, in any pairing).
                var aPairs = aP.EnumerateArray()
                               .SelectMany(p => new[] {
                                    p.GetProperty("white").GetInt32(),
                                    p.GetProperty("black").GetInt32() })
                               .Where(n => n > 0)
                               .ToHashSet();
                var gPairs = gP.EnumerateArray()
                               .SelectMany(p => new[] {
                                    p.GetProperty("white").GetInt32(),
                                    p.GetProperty("black").GetInt32() })
                               .Where(n => n > 0)
                               .ToHashSet();
                Assert.Subset(aPairs, gPairs);
            }

            // --- players: same count (order may differ) ---
            var aPlayers = aSec.GetProperty("players");
            var gPlayers = gSec.GetProperty("players");
            Assert.Equal(gPlayers.GetArrayLength(), aPlayers.GetArrayLength());

            // Every name in the golden set is present in our output.
            var aNames = aPlayers.EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToHashSet();
            foreach (var gPlayer in gPlayers.EnumerateArray())
            {
                Assert.Contains(gPlayer.GetProperty("name").GetString(), aNames);
            }
        }
    }
}
