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
    public async Task AddPlayer_pre_round_one_slots_into_rating_correct_pair_number()
    {
        // Late Alice rating 1500 sits between fixture pair #7 Saripalli
        // (rt 1541) and pair #8 Rahul (rt 1483), so post-add she should
        // BE the new pair #8 with everyone from #8 onwards bumping
        // down by one slot. Pair #1-#7 stay put because their ratings
        // are higher.
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var preCount = t.Sections[0].Players.Count;

        t = TournamentMutations.AddPlayer(
            t, name,
            name: "Late Alice",
            uscfId: "12345678",
            rating: 1500,
            secondaryRating: null,
            membershipExpiration: null,
            club: null, state: null, team: null, email: null, phone: null);

        var section = t.Sections.First(s => s.Name == name);
        Assert.Equal(preCount + 1, section.Players.Count);

        var alice = section.Players.Single(p => p.Name == "Late Alice");
        Assert.Equal(8, alice.PairNumber);
        Assert.Empty(alice.History);

        // Pair numbers are now monotonic by rating descending and
        // dense (1..N with no gaps).
        var orderedByPair = section.Players.OrderBy(p => p.PairNumber).ToList();
        for (var i = 0; i < orderedByPair.Count; i++)
        {
            Assert.Equal(i + 1, orderedByPair[i].PairNumber);
        }
        for (var i = 1; i < orderedByPair.Count; i++)
        {
            Assert.True(
                orderedByPair[i - 1].Rating >= orderedByPair[i].Rating,
                $"pair #{orderedByPair[i - 1].PairNumber} (rt {orderedByPair[i - 1].Rating}) " +
                $"should outrank pair #{orderedByPair[i].PairNumber} (rt {orderedByPair[i].Rating})");
        }
    }

    [Fact]
    public async Task AddPlayer_pre_round_one_with_top_rating_takes_pair_one()
    {
        // Late Top rating 9999 should end up at pair #1, pushing the
        // rest down. Original pair #1 becomes pair #2.
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var originalTopName = t.Sections[0].Players.OrderByDescending(p => p.Rating).First().Name;

        t = TournamentMutations.AddPlayer(
            t, name,
            name: "Late Top", uscfId: null, rating: 9999,
            secondaryRating: null, membershipExpiration: null,
            club: null, state: null, team: null, email: null, phone: null);

        var section = t.Sections.First(s => s.Name == name);
        var top = section.Players.Single(p => p.PairNumber == 1);
        Assert.Equal("Late Top", top.Name);
        Assert.Equal(9999, top.Rating);

        var oldTop = section.Players.Single(p => p.Name == originalTopName);
        Assert.Equal(2, oldTop.PairNumber);
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
            // Pre-round-1 re-rank: find by name, not by ".Last()" --
            // with rating 1234 the new entrant slots somewhere in the
            // middle of the rating-sorted roster, not at the bottom.
            var p = sec.Players.Single(pl => pl.Name == "New Entrant");
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
