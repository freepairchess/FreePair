using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for <see cref="TournamentMutations.UpdatePlayerInfo"/> —
/// editable identity / contact fields on a player, with writer
/// round-trip coverage for every patched SwissSys key.
/// </summary>
public class UpdatePlayerInfoTests
{
    private const string ExtendedSample = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<(Tournament, string sectionName)> LoadAsync()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var raw = await new SwissSysImporter().ImportAsync(src);
        var t   = SwissSysMapper.Map(raw);
        return (t, t.Sections[0].Name);
    }

    [Fact]
    public async Task UpdatePlayerInfo_replaces_editable_fields_and_preserves_session_state()
    {
        var (t, name) = await LoadAsync();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

        t = TournamentMutations.AddRequestedBye(t, name, pn, round: 2, ByeKind.Half);
        t = TournamentMutations.UpdatePlayerInfo(
            t, name, pn,
            name: "New Name",
            uscfId: "99999999",
            rating: 1234,
            secondaryRating: 1100,
            membershipExpiration: "2030-01-01",
            club: "New Club",
            state: "CA",
            team: "New Team",
            email: "new@example.com",
            phone: "555-0100");

        var p = t.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
        Assert.Equal("New Name", p.Name);
        Assert.Equal("99999999", p.UscfId);
        Assert.Equal(1234, p.Rating);
        Assert.Equal(1100, p.SecondaryRating);
        Assert.Equal("2030-01-01", p.MembershipExpiration);
        Assert.Equal("New Club", p.Club);
        Assert.Equal("CA", p.State);
        Assert.Equal("New Team", p.Team);
        Assert.Equal("new@example.com", p.Email);
        Assert.Equal("555-0100", p.Phone);
        // Session state preserved.
        Assert.Contains(2, p.RequestedByeRounds);
    }

    [Fact]
    public async Task UpdatePlayerInfo_clears_optional_fields_when_blank()
    {
        var (t, name) = await LoadAsync();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

        t = TournamentMutations.UpdatePlayerInfo(
            t, name, pn,
            name: "Someone",
            uscfId: null,
            rating: 1500,
            secondaryRating: null,
            membershipExpiration: "  ",
            club: "",
            state: null,
            team: null,
            email: "",
            phone: null);

        var p = t.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
        Assert.Null(p.UscfId);
        Assert.Null(p.SecondaryRating);
        Assert.Null(p.MembershipExpiration);
        Assert.Null(p.Club);
        Assert.Null(p.State);
        Assert.Null(p.Team);
        Assert.Null(p.Email);
        Assert.Null(p.Phone);
    }

    [Fact]
    public async Task UpdatePlayerInfo_blank_name_throws()
    {
        var (t, name) = await LoadAsync();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

        Assert.Throws<System.ArgumentException>(() =>
            TournamentMutations.UpdatePlayerInfo(
                t, name, pn, name: "   ",
                uscfId: null, rating: 1500, secondaryRating: null,
                membershipExpiration: null, club: null, state: null,
                team: null, email: null, phone: null));
    }

    [Fact]
    public async Task Writer_round_trips_every_editable_field()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-edit-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            var name = t.Sections[0].Name;
            var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

            t = TournamentMutations.UpdatePlayerInfo(
                t, name, pn,
                name: "Round Trip",
                uscfId: "12345678",
                rating: 1800,
                secondaryRating: 1750,
                membershipExpiration: "2028-06-30",
                club: "Test Club",
                state: "WA",
                team: "Team RT",
                email: "rt@example.com",
                phone: "206-555-0101");

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            var p2 = t2.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);

            Assert.Equal("Round Trip", p2.Name);
            Assert.Equal("12345678", p2.UscfId);
            Assert.Equal(1800, p2.Rating);
            Assert.Equal(1750, p2.SecondaryRating);
            Assert.Equal("2028-06-30", p2.MembershipExpiration);
            Assert.Equal("Test Club", p2.Club);
            Assert.Equal("WA", p2.State);
            Assert.Equal("Team RT", p2.Team);
            Assert.Equal("rt@example.com", p2.Email);
            Assert.Equal("206-555-0101", p2.Phone);

            // Clearing optional fields round-trips too.
            t2 = TournamentMutations.UpdatePlayerInfo(
                t2, name, pn,
                name: "Round Trip",
                uscfId: null,
                rating: 1800,
                secondaryRating: null,
                membershipExpiration: null,
                club: null,
                state: null,
                team: null,
                email: null,
                phone: null);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t2);

            var raw3 = await new SwissSysImporter().ImportAsync(tmp);
            var t3 = SwissSysMapper.Map(raw3);
            var p3 = t3.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
            Assert.Null(p3.UscfId);
            Assert.Null(p3.SecondaryRating);
            Assert.Null(p3.Club);
            Assert.Null(p3.Email);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
