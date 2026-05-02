using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FreePair.Core.Bbp;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Constraints;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;
using Xunit.Abstractions;

namespace FreePair.Core.Tests.Uscf.Harness;

/// <summary>
/// Diagnostic A/B harness on the Puddletown subset of the USCF corpus.
/// </summary>
/// <remarks>
/// <para>Hypothesis (chasing the ~90% pair-set mismatch rate from the
/// full multi-round harness): Puddletown's TD uses
/// <see cref="Player.Team"/> to mark family / sibling groups (the
/// Sherman family alone has 15 kids in some Under_400 sections), and
/// SwissSys honours that with a same-team-avoidance pairing
/// constraint. Without applying the same constraint, our USCF engine
/// happily pairs siblings together and diverges from SwissSys's
/// careful sibling-aware shuffling.</para>
///
/// <para>This test is purely informational — it never fails. It tallies
/// pair-set match counts on Puddletown rounds 2+ under two conditions:</para>
///
/// <list type="bullet">
///   <item><b>raw</b>: <see cref="UscfPairer.Pair"/> output, no
///         post-processing.</item>
///   <item><b>+SameTeam</b>: same engine output, then
///         <see cref="PairingSwapper"/> with
///         <see cref="SameTeamConstraint"/> applied (mimics the
///         post-processing the production
///         <see cref="BbpPairingEngine"/> wrapper applies for the live
///         BBP path).</item>
/// </list>
///
/// <para>If the +SameTeam column matches dramatically more than raw,
/// the hypothesis is confirmed and the next engineering step is to
/// either (a) apply the constraint pipeline in the multi-round
/// harness too so non-Puddletown samples benefit, or (b) bake
/// same-team avoidance into the engine's matching algorithm itself
/// (rather than a post-process swap).</para>
/// </remarks>
public class UscfPuddletownDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public UscfPuddletownDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async System.Threading.Tasks.Task Puddletown_subset_pair_set_match_with_and_without_SameTeamConstraint()
    {
        var files = UscfSampleDiscovery.FinalStateFiles()
            .Where(f => Path.GetFileName(f).StartsWith("Puddletown", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            _output.WriteLine("(no Puddletown samples found — diagnostic is a no-op)");
            return;
        }

        var loader = new TournamentLoader();
        var report = new DiagnosticReport();

        foreach (var file in files)
        {
            Tournament tournament;
            try
            {
                tournament = await loader.LoadAsync(file).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                report.LoadFailures.Add($"{Rel(file)}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            foreach (var section in tournament.Sections)
            {
                if (section.SoftDeleted) continue;
                if (section.Players.Count < 2) continue;
                if (section.RoundsPlayed < 2) continue;

                for (var round = 2; round <= section.RoundsPlayed; round++)
                {
                    Process(file, tournament, section, round, report);
                }
            }
        }

        _output.WriteLine(report.Render());
    }

    private static void Process(
        string file, Tournament tournament, Section section, int round, DiagnosticReport report)
    {
        var ctx = $"{Rel(file)} :: \"{section.Name}\" :: R{round}";

        var roundEntry = section.Rounds.FirstOrDefault(r => r.Number == round);
        if (roundEntry is null) { report.Skipped++; return; }

        // Build the same roundPool the multi-round harness uses: anyone
        // paired or bye'd (any kind) in this round.
        var roundPool = new HashSet<int>();
        foreach (var p in roundEntry.Pairings)
        {
            roundPool.Add(p.WhitePair);
            roundPool.Add(p.BlackPair);
        }
        foreach (var b in roundEntry.Byes) roundPool.Add(b.PlayerPair);

        var roster = section.Players
            .Where(p => !p.SoftDeleted && roundPool.Contains(p.PairNumber))
            .ToArray();
        if (roster.Length == 0) { report.Skipped++; return; }

        // Pre-flagged byes (P4): from the round's recorded Half / Unpaired
        // entries, since RequestedByeRounds is consumed post-pairing.
        var requestedByesDict = new Dictionary<int, char>();
        foreach (var b in roundEntry.Byes)
        {
            if (b.Kind == ByeKind.Half) requestedByesDict[b.PlayerPair] = 'H';
            else if (b.Kind == ByeKind.Unpaired) requestedByesDict[b.PlayerPair] = 'Z';
        }

        UscfPairingResult produced;
        try
        {
            var trf = BuildTrfDocAtEndOfRound(tournament, section, roster, endedRound: round - 1)
                with { RequestedByes = requestedByesDict.Count == 0 ? null : requestedByesDict };
            produced = UscfPairer.Pair(trf);
        }
        catch (NotImplementedException) { report.Unimplemented++; return; }
        catch (Exception ex) { report.Errors.Add($"{ctx}: {ex.GetType().Name}: {ex.Message}"); return; }

        // Actual pair set (white/black agnostic — we just check matchups).
        var actualSet = new HashSet<(int lo, int hi)>(
            roundEntry.Pairings.Select(p => Norm(p.WhitePair, p.BlackPair)));

        // Raw produced pair set.
        var rawSet = new HashSet<(int, int)>(
            produced.Pairings.Select(p => Norm(p.WhitePair, p.BlackPair)));

        // +SameTeam: apply the post-engine constraint pipeline (same flow
        // BbpPairingEngine.GenerateNextRoundAsync runs after parsing).
        var constraints = new IPairingConstraint[] { new SameTeamConstraint() };
        var bbpPairings = produced.Pairings
            .Select(p => new BbpPairing(p.WhitePair, p.BlackPair))
            .ToArray();
        var swapped = PairingSwapper.Apply(bbpPairings, section, constraints);
        var swappedSet = new HashSet<(int, int)>(
            swapped.Pairings.Select(p => Norm(p.WhitePair, p.BlackPair)));

        // Tally.
        report.TotalCases++;
        if (rawSet.SetEquals(actualSet))      report.RawMatches++;
        if (swappedSet.SetEquals(actualSet))  report.SwappedMatches++;

        // Sibling-pair detail: did THIS round contain any same-team pair
        // in our raw output that the constraint flipped?
        var rawHasSibling = produced.Pairings.Any(p => SharesTeam(section, p.WhitePair, p.BlackPair));
        var swappedHasSibling = swapped.Pairings.Any(p => SharesTeam(section, p.WhitePair, p.BlackPair));
        if (rawHasSibling) report.RawSiblingPairs++;
        if (swappedHasSibling) report.SwappedSiblingPairs++;
        if (swapped.UnresolvedConflicts.Count > 0) report.UnresolvedConflictRounds++;
    }

    private static bool SharesTeam(Section section, int aPair, int bPair)
    {
        var a = section.Players.FirstOrDefault(p => p.PairNumber == aPair);
        var b = section.Players.FirstOrDefault(p => p.PairNumber == bPair);
        if (a is null || b is null) return false;
        if (string.IsNullOrWhiteSpace(a.Team)) return false;
        return string.Equals(a.Team, b.Team, StringComparison.OrdinalIgnoreCase);
    }

    private static (int lo, int hi) Norm(int a, int b) => a < b ? (a, b) : (b, a);

    private static string Rel(string file) =>
        Path.GetRelativePath(System.AppContext.BaseDirectory, file)
            .Replace('\\', '/');

    // ---- TRF synthesis (mirrors the multi-round harness, kept local
    //      so the diagnostic is self-contained). -----------------------

    private static TrfDocument BuildTrfDocAtEndOfRound(
        Tournament tournament, Section section, IReadOnlyList<Player> roster, int endedRound)
    {
        var trfPlayers = roster
            .OrderBy(p => p.PairNumber)
            .Select(p => new TrfPlayer(
                PairNumber: p.PairNumber,
                Name: p.Name,
                Rating: p.Rating,
                Id: p.UscfId ?? string.Empty,
                Points: ScoreThroughRound(p, endedRound),
                Rounds: BuildRoundCells(p, endedRound),
                Team: p.Team ?? string.Empty))
            .ToList();

        return new TrfDocument(
            TournamentName: tournament.Title ?? section.Name,
            StartDate: tournament.StartDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            EndDate: tournament.EndDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            TotalRounds: Math.Max(section.FinalRound, section.RoundsPlayed),
            InitialColor: 'w',
            Players: trfPlayers);
    }

    private static decimal ScoreThroughRound(Player p, int endedRound)
    {
        decimal score = 0m;
        var rounds = Math.Min(endedRound, p.History.Count);
        for (var i = 0; i < rounds; i++) score += p.History[i].Score;
        return score;
    }

    private static IReadOnlyList<TrfRoundCell> BuildRoundCells(Player p, int endedRound)
    {
        if (endedRound <= 0) return Array.Empty<TrfRoundCell>();
        var cells = new TrfRoundCell[endedRound];
        for (var r = 0; r < endedRound; r++)
        {
            cells[r] = r < p.History.Count ? MapToCell(p.History[r]) : TrfRoundCell.Empty;
        }
        return cells;
    }

    private static TrfRoundCell MapToCell(FreePair.Core.SwissSys.RoundResult rr)
    {
        var color = rr.Color switch
        {
            FreePair.Core.SwissSys.PlayerColor.White => 'w',
            FreePair.Core.SwissSys.PlayerColor.Black => 'b',
            _ => '-',
        };
        var result = rr.Kind switch
        {
            FreePair.Core.SwissSys.RoundResultKind.Win => '1',
            FreePair.Core.SwissSys.RoundResultKind.Loss => '0',
            FreePair.Core.SwissSys.RoundResultKind.Draw => '=',
            FreePair.Core.SwissSys.RoundResultKind.FullPointBye => 'U',
            FreePair.Core.SwissSys.RoundResultKind.HalfPointBye => 'H',
            FreePair.Core.SwissSys.RoundResultKind.ZeroPointBye => 'Z',
            _ => '-',
        };
        return new TrfRoundCell(rr.Opponent, color, result);
    }

    // ---- diagnostic report --------------------------------------------

    private sealed class DiagnosticReport
    {
        public int TotalCases;
        public int RawMatches;
        public int SwappedMatches;
        public int RawSiblingPairs;
        public int SwappedSiblingPairs;
        public int UnresolvedConflictRounds;
        public int Skipped;
        public int Unimplemented;
        public List<string> Errors { get; } = new();
        public List<string> LoadFailures { get; } = new();

        public string Render()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("===== Puddletown SameTeamConstraint diagnostic =====");
            sb.AppendLine($"comparable rounds          : {TotalCases}");
            sb.AppendLine($"  raw pair-set matches     : {RawMatches}  ({Pct(RawMatches, TotalCases)})");
            sb.AppendLine($"  +SameTeam pair-set match : {SwappedMatches}  ({Pct(SwappedMatches, TotalCases)})");
            sb.AppendLine($"  delta from constraint    : {SwappedMatches - RawMatches:+#;-#;0}");
            sb.AppendLine();
            sb.AppendLine($"sibling-pair output rate:");
            sb.AppendLine($"  raw rounds with same-team pair : {RawSiblingPairs}  ({Pct(RawSiblingPairs, TotalCases)})");
            sb.AppendLine($"  +SameTeam rounds with leftover : {SwappedSiblingPairs}  ({Pct(SwappedSiblingPairs, TotalCases)})");
            sb.AppendLine($"  rounds where swapper couldn't fully resolve: {UnresolvedConflictRounds}");
            sb.AppendLine();
            if (Skipped > 0)        sb.AppendLine($"skipped (no roster / no round entry) : {Skipped}");
            if (Unimplemented > 0)  sb.AppendLine($"unimplemented                        : {Unimplemented}");
            if (Errors.Count > 0)
            {
                sb.AppendLine($"errors ({Errors.Count}):");
                foreach (var e in Errors.Take(5)) sb.AppendLine("  " + e);
            }
            if (LoadFailures.Count > 0)
            {
                sb.AppendLine($"load failures ({LoadFailures.Count}):");
                foreach (var f in LoadFailures.Take(5)) sb.AppendLine("  " + f);
            }
            return sb.ToString();
        }

        private static string Pct(int num, int den) =>
            den == 0 ? "0.0%" : $"{(100.0 * num / den):0.0}%";
    }
}
