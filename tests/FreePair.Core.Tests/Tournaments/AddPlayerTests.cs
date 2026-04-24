using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for <see cref="TournamentMutations.AddPlayer"/> — new player
/// insertion with optional back-filled byes for already-paired rounds,
/// plus writer round-trip so the freshly-minted player node persists
/// to the raw <c>.sjson</c> file.
/// </summary>
public class AddPlayerTests
{
    private const string ExtendedSample = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var raw = await new SwissSysImporter().ImportAsync(src);
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task AddPlayer_pre_round_one_assigns_next_pair_and_empty_history()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var expectedPair = t.Sections[0].Players.Max(p => p.PairNumber) + 1;

        t = TournamentMutations.AddPlayer(
            t, name,
            name: "Late Alice",
            uscfId: "12345678",
            rating: 1500,
            secondaryRating: null,
            membershipExpiration: null,
            club: null, state: null, team: null, email: null, phone: null);

        var p = t.Sections.First(s => s.Name == name).Players.Last();
        Assert.Equal(expectedPair, p.PairNumber);
        Assert.Equal("Late Alice", p.Name);
        Assert.Empty(p.History);
    }

    [Fact]
    public async Task AddPlayer_after_paired_rounds_backfills_history_with_default_zero_point_byes()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        // Synthesize a 2-round-paired section state since fixtures are all pre-round-1.
        var section = t.Sections[0];
        var paired = section with { RoundsPaired = 2 };
        t = t with { Sections = [paired, .. t.Sections.Skip(1)] };

        t = TournamentMutations.AddPlayer(
            t, name,
            name: "Late Bob",
            uscfId: null, rating: 1400,
            secondaryRating: null, membershipExpiration: null,
            club: null, state: null, team: null, email: null, phone: null);

        var p = t.Sections.First(s => s.Name == name).Players.Last();
        Assert.Equal(2, p.History.Count);
        Assert.All(p.History, r => Assert.Equal(RoundResultKind.ZeroPointBye, r.Kind));
    }

    [Fact]
    public async Task AddPlayer_honours_byesForPastRounds_dictionary()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var section = t.Sections[0];
        var paired = section with { RoundsPaired = 3 };
        t = t with { Sections = [paired, .. t.Sections.Skip(1)] };

        var byes = new Dictionary<int, ByeKind>
        {
            [1] = ByeKind.Full,
            [2] = ByeKind.Half,
            [3] = ByeKind.Unpaired,
        };

        t = TournamentMutations.AddPlayer(
            t, name,
            name: "Mixed", uscfId: null, rating: 1600,
            secondaryRating: null, membershipExpiration: null,
            club: null, state: null, team: null, email: null, phone: null,
            byesForPastRounds: byes);

        var p = t.Sections.First(s => s.Name == name).Players.Last();
        Assert.Equal(RoundResultKind.FullPointBye, p.History[0].Kind);
        Assert.Equal(RoundResultKind.HalfPointBye, p.History[1].Kind);
        Assert.Equal(RoundResultKind.ZeroPointBye, p.History[2].Kind);
        Assert.Equal(1m,   p.History[0].Score);
        Assert.Equal(0.5m, p.History[1].Score);
        Assert.Equal(0m,   p.History[2].Score);
    }

    [Fact]
    public async Task Writer_creates_new_raw_player_node_for_added_player()
    {
        // End-to-end: load, add player, save, reimport — the new player
        // must appear with all edited fields intact.
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-add-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            var name = t.Sections[0].Name;
            var before = t.Sections[0].Players.Count;

            t = TournamentMutations.AddPlayer(
                t, name,
                name: "New Entrant",
                uscfId: "99999999",
                rating: 1234,
                secondaryRating: 1100,
                membershipExpiration: "2030-01-01",
                club: "Some Club",
                state: "WA",
                team: null,
                email: "new@example.com",
                phone: "555-1111");

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            var sec = t2.Sections.First(s => s.Name == name);
            Assert.Equal(before + 1, sec.Players.Count);
            var p = sec.Players.Last();
            Assert.Equal("New Entrant", p.Name);
            Assert.Equal("99999999", p.UscfId);
            Assert.Equal(1234, p.Rating);
            Assert.Equal(1100, p.SecondaryRating);
            Assert.Equal("2030-01-01", p.MembershipExpiration);
            Assert.Equal("Some Club", p.Club);
            Assert.Equal("WA", p.State);
            Assert.Null(p.Team);
            Assert.Equal("new@example.com", p.Email);
            Assert.Equal("555-1111", p.Phone);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
