using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for <see cref="TournamentMutations.AddSection"/> — new
/// section insertion into an existing tournament, plus writer
/// round-trip so the freshly-minted section node persists to the raw
/// <c>.sjson</c> file.
/// </summary>
public class AddSectionTests
{
    private const string ExtendedSample = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var raw = await new SwissSysImporter().ImportAsync(src);
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task AddSection_appends_empty_section_with_given_metadata()
    {
        var t = await LoadAsync();
        var before = t.Sections.Count;

        t = TournamentMutations.AddSection(t, "Reserve", SectionKind.Swiss,
            finalRound: 5, timeControl: "G/60;d5");

        Assert.Equal(before + 1, t.Sections.Count);
        var s = t.Sections.Last();
        Assert.Equal("Reserve", s.Name);
        Assert.Equal(SectionKind.Swiss, s.Kind);
        Assert.Equal(5, s.FinalRound);
        Assert.Equal("G/60;d5", s.TimeControl);
        Assert.Empty(s.Players);
        Assert.Empty(s.Rounds);
        Assert.Equal(0, s.RoundsPaired);
        Assert.Equal(0, s.RoundsPlayed);
    }

    [Fact]
    public async Task AddSection_rejects_duplicate_name_case_insensitively()
    {
        var t = await LoadAsync();
        var existing = t.Sections[0].Name;

        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.AddSection(t, existing.ToUpperInvariant(), SectionKind.Swiss, finalRound: 5));
    }

    [Fact]
    public async Task AddSection_rejects_invalid_args()
    {
        var t = await LoadAsync();

        Assert.Throws<System.ArgumentException>(() =>
            TournamentMutations.AddSection(t, "", SectionKind.Swiss, 5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            TournamentMutations.AddSection(t, "X", SectionKind.Swiss, 0));
        Assert.Throws<System.ArgumentException>(() =>
            TournamentMutations.AddSection(t, "X", SectionKind.Unknown, 5));
    }

    [Fact]
    public async Task Writer_creates_new_raw_section_node_and_round_trips()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-section-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            var before = t.Sections.Count;

            t = TournamentMutations.AddSection(t, "Newcomer", SectionKind.RoundRobin,
                finalRound: 7, timeControl: "G/90;d5", title: "Rated RR");

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);

            Assert.Equal(before + 1, t2.Sections.Count);
            var s = t2.Sections.First(x => x.Name == "Newcomer");
            Assert.Equal(SectionKind.RoundRobin, s.Kind);
            Assert.Equal(7, s.FinalRound);
            Assert.Equal("G/90;d5", s.TimeControl);
            Assert.Equal("Rated RR", s.Title);
            Assert.Empty(s.Players);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task AddSection_then_AddPlayer_then_writer_round_trips_together()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-section-player-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);

            t = TournamentMutations.AddSection(t, "Beginners", SectionKind.Swiss, finalRound: 4);
            t = TournamentMutations.AddPlayer(t, "Beginners",
                name: "Player One", uscfId: "11111111", rating: 800,
                secondaryRating: null, membershipExpiration: null,
                club: null, state: null, team: null, email: null, phone: null);

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            var s = t2.Sections.First(x => x.Name == "Beginners");
            Assert.Single(s.Players);
            Assert.Equal("Player One", s.Players[0].Name);
            Assert.Equal(800, s.Players[0].Rating);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
