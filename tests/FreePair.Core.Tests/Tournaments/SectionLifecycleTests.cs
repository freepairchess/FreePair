using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for the three section-lifecycle mutations
/// (<see cref="TournamentMutations.SoftDeleteSection"/>,
/// <see cref="TournamentMutations.UndeleteSection"/>,
/// <see cref="TournamentMutations.HardDeleteSection"/>) and the
/// downstream effects: mutation guards, publishing exclusion, and
/// round-trip through the SwissSys writer.
/// </summary>
public class SectionLifecycleTests
{
    private const string ExtendedSample = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var raw = await new SwissSysImporter().ImportAsync(src);
        return SwissSysMapper.Map(raw);
    }

    // ================================================================
    // SoftDeleteSection / UndeleteSection
    // ================================================================

    [Fact]
    public async Task SoftDeleteSection_sets_flag_without_discarding_data()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;

        var t2 = TournamentMutations.SoftDeleteSection(t, name);

        var s = t2.Sections.First(x => x.Name == name);
        Assert.True(s.SoftDeleted);
        Assert.Equal(t.Sections[0].Players.Count, s.Players.Count);
        Assert.Equal(t.Sections[0].Rounds.Count,  s.Rounds.Count);
    }

    [Fact]
    public async Task SoftDeleteSection_is_idempotent_only_when_asked_once()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;

        var t2 = TournamentMutations.SoftDeleteSection(t, name);

        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.SoftDeleteSection(t2, name));
    }

    [Fact]
    public async Task UndeleteSection_clears_the_flag()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var deleted = TournamentMutations.SoftDeleteSection(t, name);

        var restored = TournamentMutations.UndeleteSection(deleted, name);

        Assert.False(restored.Sections.First(x => x.Name == name).SoftDeleted);
    }

    [Fact]
    public async Task UndeleteSection_on_live_section_throws()
    {
        var t = await LoadAsync();
        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.UndeleteSection(t, t.Sections[0].Name));
    }

    // ================================================================
    // Mutation guard
    // ================================================================

    [Fact]
    public async Task Mutations_against_soft_deleted_section_throw()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        t = TournamentMutations.SoftDeleteSection(t, name);

        // A cross-section of the section-targeted mutations. They
        // should all refuse to touch a soft-deleted section.
        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.SetPairingResult(t, name, 1, 1, 2, PairingResult.Draw));
        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.SetAvoidSameClub(t, name, true));
        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.AddDoNotPair(t, name, 1, 2));
        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.DeleteLastRound(t, name));
    }

    // ================================================================
    // HardDeleteSection
    // ================================================================

    [Fact]
    public async Task HardDeleteSection_removes_the_section_entirely()
    {
        var t = await LoadAsync();
        Assert.True(t.Sections.Count >= 2, "Fixture must have >=2 sections.");
        var target = t.Sections[0].Name;
        var survivor = t.Sections[1].Name;

        var t2 = TournamentMutations.HardDeleteSection(t, target);

        Assert.DoesNotContain(t2.Sections, s => s.Name == target);
        Assert.Contains(t2.Sections, s => s.Name == survivor);
    }

    [Fact]
    public async Task HardDeleteSection_works_on_soft_deleted_sections_too()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        t = TournamentMutations.SoftDeleteSection(t, name);

        var t2 = TournamentMutations.HardDeleteSection(t, name);

        Assert.DoesNotContain(t2.Sections, s => s.Name == name);
    }

    // ================================================================
    // Publishing exclusion
    // ================================================================

    [Fact]
    public async Task Publishing_skips_soft_deleted_sections()
    {
        var t = await LoadAsync();
        Assert.True(t.Sections.Count >= 2);
        var hidden = t.Sections[0].Name;
        t = TournamentMutations.SoftDeleteSection(t, hidden);

        using var doc = JsonDocument.Parse(SwissSysResultJsonBuilder.Build(t));
        var names = doc.RootElement.GetProperty("sections")
            .EnumerateArray()
            .Select(e => e.GetProperty("section").GetString())
            .ToArray();

        Assert.DoesNotContain(hidden, names);
        // Every other section still present.
        foreach (var s in t.Sections.Where(s => !s.SoftDeleted))
        {
            Assert.Contains(s.Name, names);
        }
    }

    // ================================================================
    // SwissSys round-trip
    // ================================================================

    [Fact]
    public async Task Writer_round_trips_soft_deleted_flag()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-softdel-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            var name = t.Sections[0].Name;

            t = TournamentMutations.SoftDeleteSection(t, name);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            Assert.True(t2.Sections.First(s => s.Name == name).SoftDeleted);

            // Round-trip the other way: undelete + save removes the key.
            t2 = TournamentMutations.UndeleteSection(t2, name);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t2);

            var raw3 = await new SwissSysImporter().ImportAsync(tmp);
            var t3 = SwissSysMapper.Map(raw3);
            Assert.False(t3.Sections.First(s => s.Name == name).SoftDeleted);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Writer_propagates_hard_delete_by_pruning_raw_section()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-harddel-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            Assert.True(t.Sections.Count >= 2);
            var removed = t.Sections[0].Name;

            t = TournamentMutations.HardDeleteSection(t, removed);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            Assert.DoesNotContain(t2.Sections, s => s.Name == removed);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
