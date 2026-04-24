using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for the New-event flow: building a blank tournament via the
/// <see cref="Tournament"/> constructor and asking
/// <see cref="SwissSysTournamentWriter"/> to save it to a path that
/// doesn't yet exist. The writer's missing-file branch seeds a
/// minimal SwissSys v11 JSON scaffold so the resulting file is
/// re-openable.
/// </summary>
public class NewEventTests
{
    [Fact]
    public async Task Writer_creates_valid_sjson_for_brand_new_tournament()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-new-{System.Guid.NewGuid():N}.sjson");
        try
        {
            var tournament = new Tournament(
                Title: "Spring Open 2026",
                StartDate: null, EndDate: null,
                TimeControl: null, NachEventId: null,
                Sections: System.Array.Empty<Section>());

            // Ensure the path doesn't exist yet — we're testing the
            // scaffold branch.
            Assert.False(File.Exists(tmp));

            await new SwissSysTournamentWriter().SaveAsync(tmp, tournament);

            Assert.True(File.Exists(tmp));

            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t  = SwissSysMapper.Map(raw);
            Assert.Equal("Spring Open 2026", t.Title);
            Assert.Empty(t.Sections);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task New_event_with_starter_section_and_player_round_trips_fully()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-new-full-{System.Guid.NewGuid():N}.sjson");
        try
        {
            var tournament = new Tournament(
                Title: "Beginner Rapid",
                StartDate: null, EndDate: null,
                TimeControl: null, NachEventId: null,
                Sections: System.Array.Empty<Section>());

            tournament = TournamentMutations.AddSection(tournament, "Open", SectionKind.Swiss, finalRound: 4, timeControl: "G/30;d3");
            tournament = TournamentMutations.AddPlayer(tournament, "Open",
                name: "First Player", uscfId: null, rating: 1200,
                secondaryRating: null, membershipExpiration: null,
                club: null, state: null, team: null, email: null, phone: null);

            await new SwissSysTournamentWriter().SaveAsync(tmp, tournament);

            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t  = SwissSysMapper.Map(raw);

            Assert.Equal("Beginner Rapid", t.Title);
            Assert.Single(t.Sections);
            var s = t.Sections[0];
            Assert.Equal("Open", s.Name);
            Assert.Equal(SectionKind.Swiss, s.Kind);
            Assert.Equal(4, s.FinalRound);
            Assert.Equal("G/30;d3", s.TimeControl);
            Assert.Single(s.Players);
            Assert.Equal("First Player", s.Players[0].Name);
            Assert.Equal(1200, s.Players[0].Rating);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
