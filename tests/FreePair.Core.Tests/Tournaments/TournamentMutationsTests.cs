using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

public class TournamentMutationsTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task SetPairingResult_changes_result_and_updates_player_history()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var r1board1 = openI.Rounds[0].Pairings.Single(p => p.Board == 1);
        Assert.Equal(9, r1board1.WhitePair);
        Assert.Equal(1, r1board1.BlackPair);
        Assert.Equal(PairingResult.BlackWins, r1board1.Result);

        // Flip the result to a draw.
        var updated = TournamentMutations.SetPairingResult(
            t, "Open I", round: 1, whitePair: 9, blackPair: 1, PairingResult.Draw);

        var updatedRound1 = updated.Sections.Single(s => s.Name == "Open I").Rounds[0];
        var updatedBoard1 = updatedRound1.Pairings.Single(p => p.Board == 1);
        Assert.Equal(PairingResult.Draw, updatedBoard1.Result);

        var updatedPair1 = updated.Sections.Single(s => s.Name == "Open I")
            .Players.Single(p => p.PairNumber == 1);
        Assert.Equal(RoundResultKind.Draw, updatedPair1.History[0].Kind);

        var updatedPair9 = updated.Sections.Single(s => s.Name == "Open I")
            .Players.Single(p => p.PairNumber == 9);
        Assert.Equal(RoundResultKind.Draw, updatedPair9.History[0].Kind);
    }

    [Fact]
    public async Task SetPairingResult_leaves_other_sections_untouched()
    {
        var t = await LoadAsync();

        var updated = TournamentMutations.SetPairingResult(
            t, "Open I", 1, 9, 1, PairingResult.Draw);

        var originalOpenII = t.Sections.Single(s => s.Name == "Open II");
        var updatedOpenII = updated.Sections.Single(s => s.Name == "Open II");
        Assert.Same(originalOpenII, updatedOpenII);
    }

    [Fact]
    public async Task SetPairingResult_does_not_mutate_original_tournament()
    {
        var t = await LoadAsync();
        var originalBoard1 = t.Sections.Single(s => s.Name == "Open I")
            .Rounds[0].Pairings.Single(p => p.Board == 1);

        _ = TournamentMutations.SetPairingResult(
            t, "Open I", 1, 9, 1, PairingResult.Draw);

        Assert.Equal(PairingResult.BlackWins, originalBoard1.Result);
    }

    [Fact]
    public async Task AppendRound_adds_pairings_and_updates_history()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var bbpResult = new BbpPairingResult(
            new[]
            {
                new BbpPairing(1, 2),
                new BbpPairing(3, 4),
                new BbpPairing(5, 6),
                new BbpPairing(7, 8),
                new BbpPairing(9, 10),
                new BbpPairing(11, 12),
                new BbpPairing(13, 14),
                new BbpPairing(15, 16),
            },
            System.Array.Empty<int>());

        var updated = TournamentMutations.AppendRound(t, "Open I", bbpResult);
        var updatedOpenI = updated.Sections.Single(s => s.Name == "Open I");

        Assert.Equal(4, updatedOpenI.Rounds.Count);
        Assert.Equal(4, updatedOpenI.RoundsPaired);
        Assert.Equal(3, updatedOpenI.RoundsPlayed); // unchanged — results not in yet

        var r4 = updatedOpenI.Rounds[3];
        Assert.Equal(4, r4.Number);
        Assert.Equal(8, r4.Pairings.Count);
        Assert.All(r4.Pairings, p => Assert.Equal(PairingResult.Unplayed, p.Result));
        Assert.Equal(1, r4.Pairings[0].Board);
        Assert.Equal(8, r4.Pairings[^1].Board);

        // Each player gains one history entry.
        Assert.All(updatedOpenI.Players, p => Assert.Equal(4, p.History.Count));

        // Pair 1 should be paired with Pair 2 as White in round 4.
        var pair1 = updatedOpenI.Players.Single(p => p.PairNumber == 1);
        Assert.Equal(2, pair1.History[3].Opponent);
        Assert.Equal(PlayerColor.White, pair1.History[3].Color);
        Assert.Equal(RoundResultKind.None, pair1.History[3].Kind); // unplayed
    }

    [Fact]
    public async Task AppendRound_awards_full_point_bye_immediately()
    {
        var t = await LoadAsync();

        var bbpResult = new BbpPairingResult(
            System.Array.Empty<BbpPairing>(),
            new[] { 5 });

        var updated = TournamentMutations.AppendRound(t, "Open I", bbpResult);
        var pair5 = updated.Sections.Single(s => s.Name == "Open I")
            .Players.Single(p => p.PairNumber == 5);

        Assert.Equal(RoundResultKind.FullPointBye, pair5.History[3].Kind);
        Assert.Equal(1m, pair5.History[3].Score);
    }

    [Fact]
    public async Task AppendRound_awards_half_point_bye_for_HalfPointByePlayerPairs()
    {
        // Requested ½-pt bye: the player was pre-flagged (via a TRF 'H'
        // cell) before BBP ran, so BBP didn't pair them. TournamentMutations
        // must still stamp a HalfPointBye history entry so standings /
        // wall chart reflect the ½ point.
        var t = await LoadAsync();

        var bbpResult = new BbpPairingResult(
            System.Array.Empty<BbpPairing>(),
            ByePlayerPairs: System.Array.Empty<int>(),
            HalfPointByePlayerPairs: new[] { 7 });

        var updated = TournamentMutations.AppendRound(t, "Open I", bbpResult);
        var pair7 = updated.Sections.Single(s => s.Name == "Open I")
            .Players.Single(p => p.PairNumber == 7);

        Assert.Equal(RoundResultKind.HalfPointBye, pair7.History[3].Kind);
        Assert.Equal(0.5m, pair7.History[3].Score);

        // New round's bye assignments expose it too.
        var newRound = updated.Sections.Single(s => s.Name == "Open I")
            .Rounds.Last();
        Assert.Contains(newRound.Byes, b => b.PlayerPair == 7 && b.Kind == ByeKind.Half);
    }

    [Fact]
    public async Task IsRoundComplete_reflects_result_state()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        // All 3 Open I rounds in the sample are fully played.
        Assert.True(TournamentMutations.IsRoundComplete(openI, 1));
        Assert.True(TournamentMutations.IsRoundComplete(openI, 2));
        Assert.True(TournamentMutations.IsRoundComplete(openI, 3));
    }

    [Fact]
    public async Task SetPairingResult_on_newly_appended_round_advances_RoundsPlayed_when_all_results_in()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var bbpResult = new BbpPairingResult(
            new[]
            {
                new BbpPairing(1, 2), new BbpPairing(3, 4), new BbpPairing(5, 6),
                new BbpPairing(7, 8), new BbpPairing(9, 10), new BbpPairing(11, 12),
                new BbpPairing(13, 14), new BbpPairing(15, 16),
            },
            System.Array.Empty<int>());

        var updated = TournamentMutations.AppendRound(t, "Open I", bbpResult);
        Assert.Equal(3, updated.Sections.Single(s => s.Name == "Open I").RoundsPlayed);

        // Enter every round-4 result.
        var pairs = new (int w, int b)[] {
            (1, 2), (3, 4), (5, 6), (7, 8), (9, 10), (11, 12), (13, 14), (15, 16),
        };
        foreach (var (w, b) in pairs)
        {
            updated = TournamentMutations.SetPairingResult(
                updated, "Open I", round: 4, whitePair: w, blackPair: b,
                PairingResult.WhiteWins);
        }

        var openIFinal = updated.Sections.Single(s => s.Name == "Open I");
        Assert.Equal(4, openIFinal.RoundsPlayed);
        Assert.True(TournamentMutations.IsRoundComplete(openIFinal, 4));
    }

    [Fact]
    public async Task DeleteLastRound_pops_round_pairings_byes_and_history()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");
        Assert.Equal(3, openI.Rounds.Count);
        Assert.Equal(3, openI.RoundsPaired);
        Assert.Equal(3, openI.RoundsPlayed);
        Assert.All(openI.Players, p => Assert.Equal(3, p.History.Count));

        var updated = TournamentMutations.DeleteLastRound(t, "Open I");
        var trimmed = updated.Sections.Single(s => s.Name == "Open I");

        Assert.Equal(2, trimmed.Rounds.Count);
        Assert.Equal(2, trimmed.RoundsPaired);
        Assert.Equal(2, trimmed.RoundsPlayed);
        Assert.All(trimmed.Players, p => Assert.Equal(2, p.History.Count));
        Assert.DoesNotContain(trimmed.Rounds, r => r.Number == 3);
    }

    [Fact]
    public async Task DeleteLastRound_does_not_mutate_original_tournament()
    {
        var t = await LoadAsync();
        var originalCount = t.Sections.Single(s => s.Name == "Open I").Rounds.Count;

        _ = TournamentMutations.DeleteLastRound(t, "Open I");

        Assert.Equal(originalCount, t.Sections.Single(s => s.Name == "Open I").Rounds.Count);
    }

    [Fact]
    public async Task DeleteLastRound_throws_when_no_rounds_exist()
    {
        var t = await LoadAsync();

        // Drop all 3 rounds, then the 4th call should throw.
        var r1 = TournamentMutations.DeleteLastRound(t, "Open I");
        var r2 = TournamentMutations.DeleteLastRound(r1, "Open I");
        var r3 = TournamentMutations.DeleteLastRound(r2, "Open I");

        Assert.Empty(r3.Sections.Single(s => s.Name == "Open I").Rounds);
        Assert.Throws<InvalidOperationException>(
            () => TournamentMutations.DeleteLastRound(r3, "Open I"));
    }

    [Fact]
    public async Task DeleteLastRound_then_AppendRound_restores_a_fresh_round()
    {
        var t = await LoadAsync();

        var trimmed = TournamentMutations.DeleteLastRound(t, "Open I");
        Assert.Equal(2, trimmed.Sections.Single(s => s.Name == "Open I").Rounds.Count);

        var bbp = new BbpPairingResult(
            new[]
            {
                new BbpPairing(1, 2), new BbpPairing(3, 4), new BbpPairing(5, 6),
                new BbpPairing(7, 8), new BbpPairing(9, 10), new BbpPairing(11, 12),
                new BbpPairing(13, 14), new BbpPairing(15, 16),
            },
            System.Array.Empty<int>());
        var appended = TournamentMutations.AppendRound(trimmed, "Open I", bbp);

        var openIFinal = appended.Sections.Single(s => s.Name == "Open I");
        Assert.Equal(3, openIFinal.Rounds.Count);
        Assert.Equal(3, openIFinal.RoundsPaired);
        Assert.Equal(2, openIFinal.RoundsPlayed); // new round 3 has no results yet
        Assert.All(openIFinal.Rounds[2].Pairings,
            p => Assert.Equal(PairingResult.Unplayed, p.Result));
    }
}
