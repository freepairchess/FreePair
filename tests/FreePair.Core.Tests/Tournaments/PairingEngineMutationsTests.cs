using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for <see cref="TournamentMutations.SetTournamentPairingEngine"/>,
/// <see cref="TournamentMutations.SetSectionPairingEngine"/>, and
/// <see cref="PairingEngineDefaults"/>. Covers the rating-type-derived
/// default cascade and the "lock once a round is paired" invariant.
/// </summary>
public class PairingEngineMutationsTests
{
    private const string FreshSampleFileName = "Boston_Queens_Series_Event_1_Start.sjson";

    private static async Task<Tournament> LoadFreshAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.UscfSample("library", FreshSampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public void Default_for_FIDE_rating_type_is_Bbp()
    {
        Assert.Equal(PairingEngineKind.Bbp, PairingEngineDefaults.ForRatingType(RatingType.FIDE));
        Assert.Equal(PairingEngineKind.Bbp, PairingEngineDefaults.ForRatingType(RatingType.USCF_FIDE));
        Assert.Equal(PairingEngineKind.Bbp, PairingEngineDefaults.ForRatingType(RatingType.CFC_FIDE));
        Assert.Equal(PairingEngineKind.Bbp, PairingEngineDefaults.ForRatingType(RatingType.USCF_FIDE_NW));
        Assert.Equal(PairingEngineKind.Bbp, PairingEngineDefaults.ForRatingType(RatingType.USCF_CFC_FIDE_NW));
    }

    [Fact]
    public void Default_for_non_FIDE_rating_types_is_Uscf()
    {
        Assert.Equal(PairingEngineKind.Uscf, PairingEngineDefaults.ForRatingType(RatingType.USCF));
        Assert.Equal(PairingEngineKind.Uscf, PairingEngineDefaults.ForRatingType(RatingType.USCFONLINE));
        Assert.Equal(PairingEngineKind.Uscf, PairingEngineDefaults.ForRatingType(RatingType.USCF_NW));
        Assert.Equal(PairingEngineKind.Uscf, PairingEngineDefaults.ForRatingType(RatingType.CFC));
        Assert.Equal(PairingEngineKind.Uscf, PairingEngineDefaults.ForRatingType(RatingType.UnRated));
        Assert.Equal(PairingEngineKind.Uscf, PairingEngineDefaults.ForRatingType(null));
    }

    [Fact]
    public async Task Resolve_uses_section_override_when_set()
    {
        var t = await LoadFreshAsync();
        var sectionName = t.Sections[0].Name;

        // Pin the tournament to USCF, but section to BBP — section wins.
        t = TournamentMutations.SetTournamentPairingEngine(t, PairingEngineKind.Uscf);
        t = TournamentMutations.SetSectionPairingEngine(t, sectionName, PairingEngineKind.Bbp);

        var section = t.Sections.Single(s => s.Name == sectionName);
        Assert.Equal(PairingEngineKind.Bbp, PairingEngineDefaults.Resolve(t, section));
    }

    [Fact]
    public async Task Resolve_falls_back_to_tournament_then_rating_type()
    {
        var t = await LoadFreshAsync();
        var section = t.Sections[0];

        // Both nulled → derive from rating type. Boston Queens Series
        // is USCF-only (rating type "USCF") → Uscf engine.
        Assert.Null(section.PairingEngine);
        Assert.Null(t.PairingEngine);
        Assert.Equal(PairingEngineKind.Uscf, PairingEngineDefaults.Resolve(t, section));

        // Pin tournament to Bbp → section now resolves to Bbp.
        var tBbp = TournamentMutations.SetTournamentPairingEngine(t, PairingEngineKind.Bbp);
        var sectionBbp = tBbp.Sections.Single(s => s.Name == section.Name);
        Assert.Equal(PairingEngineKind.Bbp, PairingEngineDefaults.Resolve(tBbp, sectionBbp));
    }

    [Fact]
    public async Task SetTournamentPairingEngine_throws_when_any_section_has_paired_a_round()
    {
        // Use a sample that has actual paired rounds.
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(
            TestPaths.SwissSysSample("Chess_A2Z_April_Open_2026_SwissSys11.sjson"));
        var t = SwissSysMapper.Map(raw);

        // Sanity: this fixture has at least one section with a paired round.
        Assert.Contains(t.Sections, s => s.RoundsPaired > 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TournamentMutations.SetTournamentPairingEngine(t, PairingEngineKind.Uscf));
        Assert.Contains("Cannot change", ex.Message);
        Assert.Contains("after a round has been paired", ex.Message);
    }

    [Fact]
    public async Task SetSectionPairingEngine_throws_when_section_has_paired_a_round()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(
            TestPaths.SwissSysSample("Chess_A2Z_April_Open_2026_SwissSys11.sjson"));
        var t = SwissSysMapper.Map(raw);
        var paired = t.Sections.First(s => s.RoundsPaired > 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TournamentMutations.SetSectionPairingEngine(t, paired.Name, PairingEngineKind.Uscf));
        Assert.Contains(paired.Name, ex.Message);
        Assert.Contains("after round 1 is paired", ex.Message);
    }

    [Fact]
    public async Task SetTournamentPairingEngine_succeeds_when_no_section_has_paired()
    {
        var t = await LoadFreshAsync();
        Assert.All(t.Sections, s => Assert.Equal(0, s.RoundsPaired));

        var updated = TournamentMutations.SetTournamentPairingEngine(t, PairingEngineKind.Uscf);
        Assert.Equal(PairingEngineKind.Uscf, updated.PairingEngine);

        // Clear back to null.
        var cleared = TournamentMutations.SetTournamentPairingEngine(updated, null);
        Assert.Null(cleared.PairingEngine);
    }

    [Fact]
    public async Task SetSectionPairingEngine_succeeds_for_unpaired_section()
    {
        var t = await LoadFreshAsync();
        var sectionName = t.Sections[0].Name;

        var updated = TournamentMutations.SetSectionPairingEngine(t, sectionName, PairingEngineKind.Uscf);
        var section = updated.Sections.Single(s => s.Name == sectionName);
        Assert.Equal(PairingEngineKind.Uscf, section.PairingEngine);
    }

    [Fact]
    public async Task PairingEngine_round_trips_through_SwissSys_writer_and_mapper()
    {
        // Load a fresh tournament, set both tournament-level and
        // section-level overrides, write to a temp file, re-read, and
        // confirm the mapper recovered the same values.
        var t = await LoadFreshAsync();
        var sectionName = t.Sections[0].Name;

        t = TournamentMutations.SetTournamentPairingEngine(t, PairingEngineKind.Uscf);
        t = TournamentMutations.SetSectionPairingEngine(t, sectionName, PairingEngineKind.Bbp);

        // Round-trip through the writer (pass-through-style — re-opens
        // the original file and patches keys, so we need a temp copy
        // of the source to avoid mutating the fixture).
        var srcPath = TestPaths.UscfSample("library", FreshSampleFileName);
        var tmpPath = Path.Combine(Path.GetTempPath(), $"freepair-pe-rt-{Guid.NewGuid():N}.sjson");
        File.Copy(srcPath, tmpPath, overwrite: true);
        try
        {
            var writer = new SwissSysTournamentWriter();
            await writer.SaveAsync(tmpPath, t);

            // Now re-read.
            var importer = new SwissSysImporter();
            var raw = await importer.ImportAsync(tmpPath);
            var reloaded = SwissSysMapper.Map(raw);

            Assert.Equal(PairingEngineKind.Uscf, reloaded.PairingEngine);
            var reloadedSection = reloaded.Sections.Single(s => s.Name == sectionName);
            Assert.Equal(PairingEngineKind.Bbp, reloadedSection.PairingEngine);

            // Other sections (no override) must remain null.
            foreach (var s in reloaded.Sections.Where(s => s.Name != sectionName))
            {
                Assert.Null(s.PairingEngine);
            }
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    [Fact]
    public async Task Clearing_PairingEngine_overrides_removes_keys_from_persisted_file()
    {
        // Sanity: writing a Tournament with PairingEngine=null leaves
        // the .sjson clean (no stale "FreePair pairing engine" key).
        var t = await LoadFreshAsync();
        Assert.Null(t.PairingEngine);

        var srcPath = TestPaths.UscfSample("library", FreshSampleFileName);
        var tmpPath = Path.Combine(Path.GetTempPath(), $"freepair-pe-clear-{Guid.NewGuid():N}.sjson");
        File.Copy(srcPath, tmpPath, overwrite: true);
        try
        {
            var writer = new SwissSysTournamentWriter();
            await writer.SaveAsync(tmpPath, t);

            var json = await File.ReadAllTextAsync(tmpPath);
            Assert.DoesNotContain("\"FreePair pairing engine\"", json);
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }
}
