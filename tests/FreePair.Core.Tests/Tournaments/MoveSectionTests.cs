using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for <see cref="TournamentMutations.MoveSection"/> — list
/// reordering mutation plus writer round-trip so the new order
/// persists to the raw <c>.sjson</c> file.
/// </summary>
public class MoveSectionTests
{
    private const string ExtendedSample = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var raw = await new SwissSysImporter().ImportAsync(src);
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task MoveSection_noop_when_already_at_boundary()
    {
        var t = await LoadAsync();
        t = TournamentMutations.AddSection(t, "A", SectionKind.Swiss, 5);
        var firstName = t.Sections[0].Name;

        var moved = TournamentMutations.MoveSection(t, firstName, -1);
        Assert.Same(t, moved); // no-op reference equality on the same record
    }

    [Fact]
    public async Task MoveSection_shifts_by_one()
    {
        var t = await LoadAsync();
        // Guarantee 3 sections for the test.
        t = TournamentMutations.AddSection(t, "A", SectionKind.Swiss, 5);
        t = TournamentMutations.AddSection(t, "B", SectionKind.Swiss, 5);
        var names0 = t.Sections.Select(s => s.Name).ToArray();

        t = TournamentMutations.MoveSection(t, names0[^1], -1);
        var names1 = t.Sections.Select(s => s.Name).ToArray();
        Assert.Equal(names0[^1], names1[^2]);
        Assert.Equal(names0[^2], names1[^1]);
    }

    [Fact]
    public async Task MoveSection_clamps_large_deltas_to_bounds()
    {
        var t = await LoadAsync();
        t = TournamentMutations.AddSection(t, "A", SectionKind.Swiss, 5);
        var firstName = t.Sections[0].Name;

        t = TournamentMutations.MoveSection(t, firstName, delta: 999);
        Assert.Equal(firstName, t.Sections[^1].Name);
    }

    [Fact]
    public async Task Writer_round_trips_section_reorder()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-reorder-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            t = TournamentMutations.AddSection(t, "X", SectionKind.Swiss, 4);
            t = TournamentMutations.AddSection(t, "Y", SectionKind.Swiss, 4);

            // Move the last section to the top.
            var targetName = t.Sections[^1].Name;
            t = TournamentMutations.MoveSection(t, targetName, -999);
            var expectedOrder = t.Sections.Select(s => s.Name).ToArray();

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            Assert.Equal(expectedOrder, t2.Sections.Select(s => s.Name).ToArray());
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
