using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for the requested-bye mutations
/// (<see cref="TournamentMutations.AddRequestedBye"/> /
/// <see cref="TournamentMutations.RemoveRequestedBye"/>) and the
/// SwissSys round-trip of the new
/// <c>"FreePair zero-point bye rounds"</c> per-player key.
/// </summary>
public class RequestedByeTests
{
    private const string ExtendedSample = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var raw = await new SwissSysImporter().ImportAsync(src);
        return SwissSysMapper.Map(raw);
    }

    private static Player First(Tournament t, string section) =>
        t.Sections.First(s => s.Name == section).Players[0];

    [Fact]
    public async Task AddRequestedBye_Half_appends_to_RequestedByeRounds()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var pn = First(t, name).PairNumber;

        t = TournamentMutations.AddRequestedBye(t, name, pn, round: 2, ByeKind.Half);

        var p = t.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
        Assert.Contains(2, p.RequestedByeRounds);
        Assert.DoesNotContain(2, p.ZeroPointByeRoundsOrEmpty);
    }

    [Fact]
    public async Task AddRequestedBye_Unpaired_appends_to_ZeroPointByeRounds()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var pn = First(t, name).PairNumber;

        t = TournamentMutations.AddRequestedBye(t, name, pn, round: 3, ByeKind.Unpaired);

        var p = t.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
        Assert.Contains(3, p.ZeroPointByeRoundsOrEmpty);
        Assert.DoesNotContain(3, p.RequestedByeRounds);
    }

    [Fact]
    public async Task AddRequestedBye_flipping_kind_moves_round_between_lists()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var pn = First(t, name).PairNumber;

        t = TournamentMutations.AddRequestedBye(t, name, pn, round: 2, ByeKind.Half);
        t = TournamentMutations.AddRequestedBye(t, name, pn, round: 2, ByeKind.Unpaired);

        var p = t.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
        Assert.DoesNotContain(2, p.RequestedByeRounds);
        Assert.Contains(2, p.ZeroPointByeRoundsOrEmpty);
    }

    [Fact]
    public async Task RemoveRequestedBye_clears_the_entry_from_either_list()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var pn = First(t, name).PairNumber;
        t = TournamentMutations.AddRequestedBye(t, name, pn, round: 4, ByeKind.Unpaired);

        t = TournamentMutations.RemoveRequestedBye(t, name, pn, round: 4);

        var p = t.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
        Assert.Empty(p.ZeroPointByeRoundsOrEmpty);
    }

    [Fact]
    public async Task AddRequestedBye_throws_for_invalid_kind()
    {
        var t = await LoadAsync();
        var name = t.Sections[0].Name;
        var pn = First(t, name).PairNumber;

        Assert.Throws<System.ArgumentException>(() =>
            TournamentMutations.AddRequestedBye(t, name, pn, round: 2, ByeKind.Full));
    }

    [Fact]
    public async Task Writer_round_trips_zero_point_bye_rounds()
    {
        var src = TestPaths.SwissSysSample(ExtendedSample);
        var tmp = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"fp-zpb-{System.Guid.NewGuid():N}.sjson");
        System.IO.File.Copy(src, tmp, overwrite: true);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(tmp);
            var t = SwissSysMapper.Map(raw);
            var name = t.Sections[0].Name;
            var pn = First(t, name).PairNumber;
            t = TournamentMutations.AddRequestedBye(t, name, pn, round: 3, ByeKind.Unpaired);

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var raw2 = await new SwissSysImporter().ImportAsync(tmp);
            var t2 = SwissSysMapper.Map(raw2);
            var p2 = t2.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
            Assert.Contains(3, p2.ZeroPointByeRoundsOrEmpty);

            // Clearing should remove the key entirely so legacy readers
            // don't encounter a stale empty array.
            t2 = TournamentMutations.RemoveRequestedBye(t2, name, pn, round: 3);
            await new SwissSysTournamentWriter().SaveAsync(tmp, t2);

            var raw3 = await new SwissSysImporter().ImportAsync(tmp);
            var t3 = SwissSysMapper.Map(raw3);
            var p3 = t3.Sections.First(s => s.Name == name).Players.First(x => x.PairNumber == pn);
            Assert.Empty(p3.ZeroPointByeRoundsOrEmpty);
        }
        finally
        {
            if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp);
        }
    }
}
