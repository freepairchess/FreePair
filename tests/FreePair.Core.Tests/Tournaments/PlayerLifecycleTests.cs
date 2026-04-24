using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for the three player-lifecycle mutations
/// (<see cref="TournamentMutations.SoftDeletePlayer"/>,
/// <see cref="TournamentMutations.UndeletePlayer"/>,
/// <see cref="TournamentMutations.HardDeletePlayer"/>) and the
/// downstream effects: pre-round-1 guard, pair-next-round block with
/// soft-deleted players, publishing / standings exclusion, and full
/// SwissSys round-trip.
/// </summary>
public class PlayerLifecycleTests
{
    // This fixture has a section with pairings + results, which we
    // use to test the "section already paired" guard. For pre-round-1
    // tests we load then delete-last-round until rounds_paired == 0.
    private const string ExtendedSample = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var raw = await new SwissSysImporter().ImportAsync(src);
        return SwissSysMapper.Map(raw);
    }

    /// <summary>
    /// Gets a tournament whose first section has no paired rounds
    /// (peel them off until rounds_paired == 0), for pre-round-1
    /// mutation tests.
    /// </summary>
    private static async Task<(Tournament, string sectionName)> LoadPreRound1Async()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        while (t.Sections.First(s => s.Name == name).RoundsPaired > 0)
        {
            t = TournamentMutations.DeleteLastRound(t, name);
        }
        return (t, name);
    }

    // ================================================================
    // SoftDeletePlayer
    // ================================================================

    [Fact]
    public async Task SoftDeletePlayer_pre_round_one_sets_flag()
    {
        var (t, name) = await LoadPreRound1Async();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

        var t2 = TournamentMutations.SoftDeletePlayer(t, name, pn);

        var p = t2.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
        Assert.True(p.SoftDeleted);
    }

    [Fact]
    public async Task SoftDeletePlayer_after_round_one_is_paired_throws()
    {
        // Synthesize a post-round-1 section by bumping RoundsPaired.
        // The fixture's sections are all pre-round-1 so we can't
        // exercise the guard without this small shim.
        var t = await LoadAsync();
        var section = t.Sections[0];
        var paired = section with { RoundsPaired = 1 };
        t = t with { Sections = [paired, .. t.Sections.Skip(1)] };
        var pn = paired.Players[0].PairNumber;

        var ex = Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.SoftDeletePlayer(t, paired.Name, pn));
        Assert.Contains("before round 1", ex.Message);
    }

    [Fact]
    public async Task SoftDeletePlayer_twice_throws()
    {
        var (t, name) = await LoadPreRound1Async();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;
        t = TournamentMutations.SoftDeletePlayer(t, name, pn);

        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.SoftDeletePlayer(t, name, pn));
    }

    // ================================================================
    // UndeletePlayer
    // ================================================================

    [Fact]
    public async Task UndeletePlayer_clears_the_flag()
    {
        var (t, name) = await LoadPreRound1Async();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;
        t = TournamentMutations.SoftDeletePlayer(t, name, pn);

        t = TournamentMutations.UndeletePlayer(t, name, pn);

        Assert.False(t.Sections.First(s => s.Name == name)
                      .Players.First(x => x.PairNumber == pn).SoftDeleted);
    }

    [Fact]
    public async Task UndeletePlayer_on_live_player_throws()
    {
        var (t, name) = await LoadPreRound1Async();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.UndeletePlayer(t, name, pn));
    }

    // ================================================================
    // HardDeletePlayer
    // ================================================================

    [Fact]
    public async Task HardDeletePlayer_pre_round_one_removes_player()
    {
        var (t, name) = await LoadPreRound1Async();
        var before = t.Sections.First(s => s.Name == name).Players.Count;
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

        var t2 = TournamentMutations.HardDeletePlayer(t, name, pn);

        var after = t2.Sections.First(s => s.Name == name).Players;
        Assert.Equal(before - 1, after.Count);
        Assert.DoesNotContain(after, p => p.PairNumber == pn);
    }

    [Fact]
    public async Task HardDeletePlayer_after_round_one_throws()
    {
        var t = await LoadAsync();
        var section = t.Sections[0];
        var paired = section with { RoundsPaired = 1 };
        t = t with { Sections = [paired, .. t.Sections.Skip(1)] };
        var pn = paired.Players[0].PairNumber;

        Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.HardDeletePlayer(t, paired.Name, pn));
    }

    [Fact]
    public async Task HardDeletePlayer_works_on_soft_deleted_player()
    {
        var (t, name) = await LoadPreRound1Async();
        var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;
        t = TournamentMutations.SoftDeletePlayer(t, name, pn);

        var t2 = TournamentMutations.HardDeletePlayer(t, name, pn);

        Assert.DoesNotContain(t2.Sections.First(s => s.Name == name).Players,
                              p => p.PairNumber == pn);
    }

    // ================================================================
    // Pair-next-round gate
    // ================================================================

    [Fact]
    public async Task AppendRoundRobinRound_refuses_with_soft_deleted_players()
    {
        var (t, name) = await LoadPreRound1Async();
        var section = t.Sections.First(s => s.Name == name);
        // Only run the test if the section is a round-robin; otherwise
        // skip gracefully (AppendRound requires BBP which is env-dependent).
        if (section.Kind != SectionKind.RoundRobin) return;

        var pn = section.Players[0].PairNumber;
        t = TournamentMutations.SoftDeletePlayer(t, name, pn);

        var ex = Assert.Throws<System.InvalidOperationException>(() =>
            TournamentMutations.AppendRoundRobinRound(t, name));
        Assert.Contains("soft-deleted players", ex.Message);
    }

    // ================================================================
    // Publishing / standings exclusion
    // ================================================================

    [Fact]
    public async Task Publishing_omits_soft_deleted_players()
    {
        var (t, name) = await LoadPreRound1Async();
        var section = t.Sections.First(s => s.Name == name);
        var hidden = section.Players[0].PairNumber;
        var hiddenName = section.Players[0].Name;
        t = TournamentMutations.SoftDeletePlayer(t, name, hidden);

        using var doc = JsonDocument.Parse(SwissSysResultJsonBuilder.Build(t));
        // Locate the matching section in the published JSON.
        var sectionJson = doc.RootElement.GetProperty("sections")
            .EnumerateArray()
            .First(e => e.GetProperty("section").GetString() == name);

        // Neither the standings nor the players array should reference
        // the soft-deleted player.
        Assert.DoesNotContain(hidden,
            sectionJson.GetProperty("standings").EnumerateArray().Select(e => e.GetInt32()));
        Assert.DoesNotContain(hiddenName,
            sectionJson.GetProperty("players").EnumerateArray()
                .Select(e => e.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Standings_omits_soft_deleted_players()
    {
        var (t, name) = await LoadPreRound1Async();
        var section = t.Sections.First(s => s.Name == name);
        var hidden = section.Players[0].PairNumber;
        t = TournamentMutations.SoftDeletePlayer(t, name, hidden);

        var rows = FreePair.Core.Tournaments.Standings.StandingsBuilder.Build(
            t.Sections.First(s => s.Name == name));

        Assert.DoesNotContain(rows, r => r.PairNumber == hidden);
    }

    // ================================================================
    // SwissSys round-trip
    // ================================================================

    [Fact]
    public async Task Writer_round_trips_soft_deleted_flag_on_player()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-playsoft-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            var name = t.Sections[0].Name;
            // Collapse to pre-round-1.
            while (t.Sections.First(s => s.Name == name).RoundsPaired > 0)
            {
                t = TournamentMutations.DeleteLastRound(t, name);
            }
            var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

            t = TournamentMutations.SoftDeletePlayer(t, name, pn);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            Assert.True(t2.Sections.First(s => s.Name == name)
                          .Players.First(x => x.PairNumber == pn).SoftDeleted);

            // Clear flag and verify key is removed on next save.
            t2 = TournamentMutations.UndeletePlayer(t2, name, pn);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t2);

            var raw3 = await new SwissSysImporter().ImportAsync(tmp);
            var t3 = SwissSysMapper.Map(raw3);
            Assert.False(t3.Sections.First(s => s.Name == name)
                           .Players.First(x => x.PairNumber == pn).SoftDeleted);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Writer_propagates_hard_delete_by_pruning_raw_player()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-playhard-{System.Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            var name = t.Sections[0].Name;
            while (t.Sections.First(s => s.Name == name).RoundsPaired > 0)
            {
                t = TournamentMutations.DeleteLastRound(t, name);
            }
            var pn = t.Sections.First(s => s.Name == name).Players[0].PairNumber;

            t = TournamentMutations.HardDeletePlayer(t, name, pn);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            Assert.DoesNotContain(t2.Sections.First(s => s.Name == name).Players,
                                  p => p.PairNumber == pn);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
