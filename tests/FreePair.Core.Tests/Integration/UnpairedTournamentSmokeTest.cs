using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Trf;
using Xunit.Abstractions;

namespace FreePair.Core.Tests.Integration;

/// <summary>
/// Exercises the full pipeline against a user-supplied pre-pairing .sjson
/// sitting in the local Downloads folder. Skipped automatically when the
/// file is not present, so this suite remains portable.
/// </summary>
public class UnpairedTournamentSmokeTest
{
    private readonly ITestOutputHelper _output;

    public UnpairedTournamentSmokeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string ExpectedPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "Chess_A2Z_May_Open_2026.sjson");

    [Fact]
    public async Task Fresh_tournament_loads_and_gates_Pair_Next_Round_correctly()
    {
        if (!File.Exists(ExpectedPath))
        {
            _output.WriteLine($"Fixture not present at {ExpectedPath}; smoke test skipped.");
            return;
        }

        // 1. Import + map through the full pipeline.
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(ExpectedPath);
        var tournament = SwissSysMapper.Map(raw);

        _output.WriteLine($"Title: {tournament.Title}");
        _output.WriteLine($"Sections: {tournament.Sections.Count}");

        foreach (var s in tournament.Sections)
        {
            _output.WriteLine(
                $"  {s.Name,-12}  players={s.Players.Count,3}  " +
                $"paired={s.RoundsPaired}  played={s.RoundsPlayed}  " +
                $"final={s.FinalRound}  rounds.count={s.Rounds.Count}");
        }

        // 2. Sanity: we expect at least one section with zero paired rounds
        // and a target-rounds > 0 — the canonical "ready for round 1" state.
        var openingCandidates = tournament.Sections
            .Where(s => s.Players.Count >= 2 && s.RoundsPaired == 0 && s.RoundsPlayed == 0 && s.FinalRound > 0)
            .ToArray();

        Assert.NotEmpty(openingCandidates);

        // 3. Verify Pair-Next-Round gate logic allows pairing round 1.
        //    Mirrors the SectionViewModel block-reason computation.
        foreach (var s in openingCandidates)
        {
            var target = Math.Max(Math.Max(s.RoundsPaired, s.FinalRound), s.RoundsPlayed);
            Assert.True(target > 0,
                $"{s.Name}: target rounds must be > 0 for the button to enable.");
            Assert.True(s.RoundsPlayed < target,
                $"{s.Name}: RoundsPlayed ({s.RoundsPlayed}) should be < target ({target}).");
            Assert.True(s.Rounds.Count <= s.RoundsPlayed,
                $"{s.Name}: no unplayed in-progress round should block pairing.");
        }

        // 4. Export every section to TRF and verify BBP-compatible output.
        //    (BBP needs ASCII, 001 lines, and either XXR or 062.)
        foreach (var s in openingCandidates)
        {
            var trf = TrfWriter.Write(tournament, s);
            Assert.Contains("012 ", trf);
            Assert.Contains("062 ", trf);
            Assert.Contains("XXR ", trf);
            Assert.All(trf, c => Assert.InRange((int)c, 0x09, 0x7E));

            var playerLines = trf.Split('\n').Count(l => l.StartsWith("001 "));
            Assert.Equal(s.Players.Count, playerLines);

            _output.WriteLine(
                $"  TRF for {s.Name}: {trf.Length} bytes, " +
                $"{playerLines} players, ASCII-clean.");
        }

        // 5. Simulate what would happen after BBP returned round-1 pairings:
        //    pair players 1v2, 3v4, ... and give an odd player a bye. Verify
        //    our AppendRound mutation produces consistent state.
        var target0 = openingCandidates[0];
        var paired = new System.Collections.Generic.List<BbpPairing>();
        var byes   = new System.Collections.Generic.List<int>();
        var pairs = target0.Players.OrderBy(p => p.PairNumber).ToArray();
        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            paired.Add(new BbpPairing(pairs[i].PairNumber, pairs[i + 1].PairNumber));
        }
        if (pairs.Length % 2 == 1)
        {
            byes.Add(pairs[^1].PairNumber);
        }

        var mutated = TournamentMutations.AppendRound(
            tournament,
            target0.Name,
            new BbpPairingResult(paired, byes));

        var mutatedSection = mutated.Sections.Single(s => s.Name == target0.Name);

        Assert.Single(mutatedSection.Rounds);
        Assert.Equal(1, mutatedSection.Rounds[0].Number);
        Assert.Equal(1, mutatedSection.RoundsPaired);
        Assert.Equal(0, mutatedSection.RoundsPlayed); // no results yet
        Assert.Equal(paired.Count, mutatedSection.Rounds[0].Pairings.Count);
        Assert.All(mutatedSection.Rounds[0].Pairings,
            p => Assert.Equal(PairingResult.Unplayed, p.Result));
        Assert.Equal(byes.Count, mutatedSection.Rounds[0].Byes.Count);

        // Enter all results → RoundsPlayed should auto-advance to 1.
        var withResults = mutated;
        foreach (var p in mutatedSection.Rounds[0].Pairings)
        {
            withResults = TournamentMutations.SetPairingResult(
                withResults, target0.Name, 1,
                p.WhitePair, p.BlackPair, PairingResult.WhiteWins);
        }

        var completed = withResults.Sections.Single(s => s.Name == target0.Name);
        Assert.Equal(1, completed.RoundsPlayed);
        Assert.True(TournamentMutations.IsRoundComplete(completed, 1));

        _output.WriteLine(
            $"  Simulated round-1 on {target0.Name}: " +
            $"{paired.Count} boards, {byes.Count} byes, " +
            $"post-results RoundsPlayed={completed.RoundsPlayed}.");
    }
}
