using System;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Trf;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for the forced-top-board feature (SwissSys parity #10):
/// TD-specified forced pairings must be withheld from BBP's TRF and
/// prepended to the final pairings list. Also validates the mutation
/// surface (add, remove, duplicate, conflict).
/// </summary>
public class ForcedPairingTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    // ============ TRF filtering ============

    [Fact]
    public async Task Write_excludes_forced_pair_players_from_001_rows_for_target_round()
    {
        var t = await LoadAsync();
        // Open I is 3 rounds in; round 4 is the next to pair. Force
        // pair 1 (white) vs pair 2 (black) on that round.
        t = TournamentMutations.AddForcedPairing(t, "Open I",
            round: 4, whitePair: 1, blackPair: 2);

        var section = t.Sections.Single(s => s.Name == "Open I");

        var trf = TrfWriter.Write(t, section, pairingRound: 4);
        var lines = trf.Split('\n');

        // Neither pair #1 nor pair #2 should appear on a 001 row.
        Assert.DoesNotContain(lines, l => l.StartsWith($"001    1 "));
        Assert.DoesNotContain(lines, l => l.StartsWith($"001    2 "));

        // 062 count reflects the smaller pool (original count minus 2).
        var header062 = lines.Single(l => l.StartsWith("062 "));
        var expected = section.Players.Count - 2;
        Assert.Contains(expected.ToString(), header062);
    }

    [Fact]
    public async Task Write_only_filters_forced_players_for_matching_round()
    {
        var t = await LoadAsync();
        // Force a pair on round 5; when we're still pairing round 4
        // that pairing must NOT cause either player to be withheld.
        t = TournamentMutations.AddForcedPairing(t, "Open I",
            round: 5, whitePair: 1, blackPair: 2);

        var section = t.Sections.Single(s => s.Name == "Open I");
        var trf = TrfWriter.Write(t, section, pairingRound: 4);
        var lines = trf.Split('\n');

        Assert.Contains(lines, l => l.StartsWith($"001    1 "));
        Assert.Contains(lines, l => l.StartsWith($"001    2 "));
    }

    // ============ mutations ============

    [Fact]
    public async Task AddForcedPairing_is_idempotent_for_same_unordered_pair()
    {
        var t = await LoadAsync();
        t = TournamentMutations.AddForcedPairing(t, "Open I", 4, 1, 2);
        var t2 = TournamentMutations.AddForcedPairing(t, "Open I", 4, 2, 1);

        // Same section identity proves the no-op path returned unchanged.
        var count = t2.Sections.Single(s => s.Name == "Open I").ForcedPairs.Count;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddForcedPairing_rejects_overlapping_player_in_same_round()
    {
        var t = await LoadAsync();
        t = TournamentMutations.AddForcedPairing(t, "Open I", 4, 1, 2);

        // Pair #1 already forced against #2 this round; cannot also
        // force against #3.
        Assert.Throws<InvalidOperationException>(() =>
            TournamentMutations.AddForcedPairing(t, "Open I", 4, 1, 3));
    }

    [Fact]
    public async Task AddForcedPairing_allows_same_player_in_different_round()
    {
        var t = await LoadAsync();
        t = TournamentMutations.AddForcedPairing(t, "Open I", 4, 1, 2);
        t = TournamentMutations.AddForcedPairing(t, "Open I", 5, 1, 3);

        var section = t.Sections.Single(s => s.Name == "Open I");
        Assert.Equal(2, section.ForcedPairs.Count);
    }

    [Fact]
    public async Task AddForcedPairing_throws_on_self_pair()
    {
        var t = await LoadAsync();
        Assert.Throws<ArgumentException>(() =>
            TournamentMutations.AddForcedPairing(t, "Open I", 4, 5, 5));
    }

    [Fact]
    public async Task RemoveForcedPairing_removes_by_unordered_pair_for_round()
    {
        var t = await LoadAsync();
        t = TournamentMutations.AddForcedPairing(t, "Open I", 4, 1, 2);
        t = TournamentMutations.RemoveForcedPairing(t, "Open I", 4, 2, 1);

        var section = t.Sections.Single(s => s.Name == "Open I");
        Assert.Empty(section.ForcedPairs);
    }

    [Fact]
    public async Task RemoveForcedPairing_is_noop_when_not_present()
    {
        var t = await LoadAsync();
        var t2 = TournamentMutations.RemoveForcedPairing(t, "Open I", 4, 1, 2);
        Assert.Same(t, t2);
    }
}
