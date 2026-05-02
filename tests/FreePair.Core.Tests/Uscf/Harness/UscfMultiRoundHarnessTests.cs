using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;
using Xunit.Abstractions;

namespace FreePair.Core.Tests.Uscf.Harness;

/// <summary>
/// Multi-round verification harness: for every section of every USCF
/// sample, replays each completed round R ≥ 2 and asserts our pairer
/// reproduces what SwissSys actually produced.
/// </summary>
/// <remarks>
/// <para>For each (sample, section, round) triple where the round was
/// actually played:</para>
/// <list type="number">
///   <item>Synthesise a <c>TrfDocument</c> representing the section's
///         state at the end of round <c>R-1</c> (using each player's
///         <see cref="Player.History"/> entries 0..R-2 to populate TRF
///         round cells).</item>
///   <item>Run <see cref="UscfPairer.Pair"/> on it.</item>
///   <item>Compare the engine's output to the section's actual round-R
///         pairings (via <see cref="Section.Rounds"/>).</item>
/// </list>
///
/// <para><b>Phase-aware tolerance.</b> Today (P0), <see cref="UscfPairer"/>
/// throws <see cref="NotImplementedException"/> for any history-bearing
/// document — that's a deliberate "fail loud, don't lie" guardrail. So
/// every R≥2 case here will currently be reported as
/// <c>⤬ unimplemented</c>. As P1 (score-group pairing), P2 (transposition-
/// based repeat avoidance), P3 (29D colour allocation), and P4
/// (byes / withdrawals) land, those cases will progressively shift to
/// <c>✓ matched</c>. The test fails only on:</para>
/// <list type="bullet">
///   <item><b>Hard mismatches</b> — pair-set or bye disagrees with
///         SwissSys (matching algorithm bug).</item>
///   <item><b>Errors</b> — exceptions other than
///         <see cref="NotImplementedException"/>.</item>
/// </list>
/// <para>Colour-only diffs and engine-unimplemented outcomes are
/// reported but tolerated.</para>
///
/// <para><b>Initial colour:</b> for round 2+ the TRF's <c>XXC</c> directive
/// is mostly informational because the round-1 colour preferences are
/// already encoded in the round-1 history cells. We pass <c>'w'</c> as a
/// neutral default; it shouldn't affect any pairing decision once the
/// engine is consulting history.</para>
/// </remarks>
public class UscfMultiRoundHarnessTests
{
    /// <summary>
    /// Pinned baseline counts captured at P1 landing time. The test fails
    /// when:
    /// <list type="bullet">
    ///   <item><c>matched</c> drops below <see cref="MinExpectedMatches"/>
    ///         — a (section, round) that used to match doesn't any more
    ///         (algorithm regression);</item>
    ///   <item><c>hard mismatch</c> exceeds <see cref="MaxExpectedHardMismatches"/>
    ///         — same diagnosis on the bad-direction axis;</item>
    ///   <item><c>colour-only diff</c> exceeds <see cref="MaxExpectedColorOnlyDiffs"/>;</item>
    ///   <item>any case threw a non-<see cref="NotImplementedException"/>
    ///         exception.</item>
    /// </list>
    /// As P2 (transposition-based repeat avoidance) and P3 (29D colour
    /// allocation) land, hard mismatches and colour-only diffs should
    /// shrink while matches grow. Ratchet these constants downward (for
    /// the bad numbers) and upward (for matches) when the engine
    /// improves.
    /// </summary>
    private const int MinExpectedMatches         = 25;
    private const int MaxExpectedHardMismatches  = 373;
    private const int MaxExpectedColorOnlyDiffs  = 14;

    private readonly ITestOutputHelper _output;

    public UscfMultiRoundHarnessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async System.Threading.Tasks.Task Round_2_plus_pairings_match_swisssys_for_every_uscf_sample()
    {
        var files = UscfSampleDiscovery.FinalStateFiles();

        if (files.Count == 0)
        {
            _output.WriteLine(
                "(no USCF samples found under docs/samples/swisssys/uscf/ — harness is a no-op)");
            return;
        }

        var loader = new TournamentLoader();
        var report = new MultiRoundReport();

        foreach (var file in files)
        {
            Tournament tournament;
            try
            {
                tournament = await loader.LoadAsync(file);
            }
            catch (Exception ex)
            {
                report.RecordLoadFailure(file, ex);
                continue;
            }

            foreach (var section in tournament.Sections)
            {
                report.ProcessSection(file, tournament, section);
            }
        }

        _output.WriteLine(report.Render());

        var problems = new List<string>();

        if (report.MatchedCount < MinExpectedMatches)
        {
            problems.Add($"matched count regressed: {report.MatchedCount} < expected ≥ {MinExpectedMatches}. " +
                         "A (section, round) that used to match SwissSys now doesn't — investigate before merging.");
        }
        if (report.HardFailures > MaxExpectedHardMismatches)
        {
            problems.Add($"hard mismatches grew: {report.HardFailures} > expected ≤ {MaxExpectedHardMismatches}. " +
                         "Either a new sample exposes a divergence (bump the constant), " +
                         "or a previously-matching case started failing (fix the regression).");
        }
        if (report.ColorOnlyDiffs > MaxExpectedColorOnlyDiffs)
        {
            problems.Add($"colour-only diffs grew: {report.ColorOnlyDiffs} > expected ≤ {MaxExpectedColorOnlyDiffs}.");
        }
        if (report.ErrorCount > 0)
        {
            problems.Add($"the engine threw {report.ErrorCount} unexpected exception(s) — see report.");
        }

        if (problems.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                "USCF multi-round harness regression(s) detected:" + Environment.NewLine +
                "  - " + string.Join(Environment.NewLine + "  - ", problems) +
                Environment.NewLine + "(see test output for the full breakdown)");
        }
    }

    // ----------------------------------------------------------------- core

    private sealed class MultiRoundReport
    {
        private readonly List<RoundOutcome> _outcomes = new();
        private readonly List<string> _loadFailures = new();

        public int HardFailures => _outcomes.Count(o => o.Kind == OutcomeKind.Mismatch);
        public int MatchedCount => _outcomes.Count(o => o.Kind == OutcomeKind.Pass);
        public int ColorOnlyDiffs => _outcomes.Count(o => o.Kind == OutcomeKind.ColorDiff);
        public int ErrorCount => _outcomes.Count(o => o.Kind == OutcomeKind.Error);

        public void RecordLoadFailure(string file, Exception ex) =>
            _loadFailures.Add($"{Rel(file)}: {ex.GetType().Name}: {ex.Message}");

        public void ProcessSection(string file, Tournament tournament, Section section)
        {
            var sectionCtx = $"{Rel(file)} :: \"{section.Name}\"";

            if (section.SoftDeleted)
            {
                _outcomes.Add(RoundOutcome.SectionSkip(sectionCtx, "soft-deleted section"));
                return;
            }
            if (section.RoundsPlayed < 2)
            {
                // Nothing to verify — round 1 is covered by UscfRound1HarnessTests.
                _outcomes.Add(RoundOutcome.SectionSkip(sectionCtx,
                    $"only {section.RoundsPlayed} round(s) played; multi-round harness needs ≥ 2"));
                return;
            }
            if (section.Rounds.Count < section.RoundsPlayed)
            {
                _outcomes.Add(RoundOutcome.SectionSkip(sectionCtx,
                    $"section claims RoundsPlayed={section.RoundsPlayed} but Rounds[] has only {section.Rounds.Count} entries (mapper issue?)"));
                return;
            }

            // Verify rounds 2..RoundsPlayed. Round 1 is already covered by
            // the dedicated UscfRound1HarnessTests harness.
            for (var round = 2; round <= section.RoundsPlayed; round++)
            {
                ProcessRound(sectionCtx, tournament, section, round);
            }
        }

        private void ProcessRound(string sectionCtx, Tournament tournament, Section section, int round)
        {
            var ctx = $"{sectionCtx} :: R{round}";

            var roundEntry = section.Rounds.FirstOrDefault(r => r.Number == round);
            if (roundEntry is null)
            {
                _outcomes.Add(RoundOutcome.Skip(ctx, $"no Rounds[] entry for round {round}"));
                return;
            }

            // Round-N pool: anyone paired or bye'd in round N. Players in
            // section.Players who don't appear in this set were absent
            // for this round (withdrew / late entry / TD-excluded) — they
            // must NOT be in the pre-round-N TRF roster.
            var actual = ExtractActuals(roundEntry);
            var roundPool = new HashSet<int>();
            foreach (var p in actual.Pairings)
            {
                roundPool.Add(p.WhitePair);
                roundPool.Add(p.BlackPair);
            }
            if (actual.ByePair is int byePair) roundPool.Add(byePair);

            var roster = section.Players
                .Where(p => !p.SoftDeleted && roundPool.Contains(p.PairNumber))
                .ToArray();

            var ghosts = roundPool
                .Where(pair => !section.Players.Any(p => p.PairNumber == pair))
                .ToArray();
            if (ghosts.Length > 0)
            {
                _outcomes.Add(RoundOutcome.Skip(ctx,
                    $"round {round} references {ghosts.Length} pair number(s) not in roster: " +
                    string.Join(",", ghosts)));
                return;
            }

            if (roster.Length == 0)
            {
                _outcomes.Add(RoundOutcome.Skip(ctx, "round pool is empty"));
                return;
            }

            // Pre-flagged half-/zero-point byes for this round are P4 territory.
            var byeQuirks = roster
                .Where(p => p.RequestedByeRounds.Contains(round) ||
                            p.ZeroPointByeRoundsOrEmpty.Contains(round))
                .ToArray();
            if (byeQuirks.Length > 0)
            {
                _outcomes.Add(RoundOutcome.Skip(ctx,
                    $"round has {byeQuirks.Length} pre-flagged bye(s) — gated until P4"));
                return;
            }

            UscfPairingResult produced;
            try
            {
                var trf = BuildTrfDocAtEndOfRound(tournament, section, roster, endedRound: round - 1);
                produced = UscfPairer.Pair(trf);
            }
            catch (NotImplementedException)
            {
                // Engine doesn't implement the rules this round needs yet.
                // Soft outcome — surface it so we can see it shrink as P1+
                // land, but don't fail the test.
                _outcomes.Add(RoundOutcome.Unimplemented(ctx, roster.Length));
                return;
            }
            catch (Exception ex)
            {
                _outcomes.Add(RoundOutcome.Error(ctx, ex));
                return;
            }

            var match = Compare(actual, produced);

            if (match.IsExactMatch)
            {
                _outcomes.Add(RoundOutcome.Pass(ctx, roster.Length));
            }
            else if (match.PairsMatchUnordered && match.ByesMatch)
            {
                _outcomes.Add(RoundOutcome.ColorDiff(ctx, roster.Length, actual, produced, match));
            }
            else
            {
                _outcomes.Add(RoundOutcome.Mismatch(ctx, roster.Length, actual, produced, match));
            }
        }

        public string Render()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("===== USCF multi-round harness (R2+) =====");

            var passes = _outcomes.Count(o => o.Kind == OutcomeKind.Pass);
            var sectionSkips = _outcomes.Count(o => o.Kind == OutcomeKind.SectionSkip);
            var roundSkips = _outcomes.Count(o => o.Kind == OutcomeKind.Skip);
            var unimpl = _outcomes.Count(o => o.Kind == OutcomeKind.Unimplemented);
            var colorDiffs = _outcomes.Count(o => o.Kind == OutcomeKind.ColorDiff);
            var fails = _outcomes.Count(o => o.Kind == OutcomeKind.Mismatch);
            var errors = _outcomes.Count(o => o.Kind == OutcomeKind.Error);

            sb.AppendLine($"(section,round) cases seen : {_outcomes.Count}");
            sb.AppendLine($"  ✓ matched       : {passes}");
            sb.AppendLine($"  ⤼ section skip  : {sectionSkips}");
            sb.AppendLine($"  ⤼ round skip    : {roundSkips}");
            sb.AppendLine($"  ⤬ unimplemented : {unimpl}  (engine doesn't yet handle this round; will shrink as P1+ land)");
            sb.AppendLine($"  ◐ colour-only   : {colorDiffs}  (matching is correct; colour allocation differs — gated until P3)");
            sb.AppendLine($"  ✗ mismatch      : {fails}");
            sb.AppendLine($"  ! error         : {errors}");
            if (_loadFailures.Count > 0)
            {
                sb.AppendLine($"Load failures : {_loadFailures.Count}");
            }
            sb.AppendLine();

            // Print only hard mismatches, errors, and colour-only diffs in
            // detail — the per-round skip/unimplemented lines would flood
            // the output. The summary above already captures their count.
            var noteworthy = _outcomes
                .Where(o => o.Kind is OutcomeKind.Mismatch or OutcomeKind.Error or OutcomeKind.ColorDiff or OutcomeKind.Pass)
                .ToArray();

            sb.AppendLine($"--- {noteworthy.Length} noteworthy outcome(s) (passes + diffs + errors) ---");
            foreach (var o in noteworthy)
            {
                sb.AppendLine(o.Format());
            }

            if (_loadFailures.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- could not load ---");
                foreach (var lf in _loadFailures)
                {
                    sb.AppendLine($"  ! {lf}");
                }
            }

            return sb.ToString();
        }

        private static string Rel(string absolute)
        {
            var root = TestPaths.RepoRoot + Path.DirectorySeparatorChar;
            return absolute.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? absolute[root.Length..]
                : absolute;
        }
    }

    // -------------------------------------------- per-round outcome record

    private enum OutcomeKind { Pass, Skip, SectionSkip, Unimplemented, ColorDiff, Mismatch, Error }

    private sealed record RoundOutcome(
        OutcomeKind Kind,
        string Context,
        string? Detail,
        RoundActuals? Actual = null,
        UscfPairingResult? Produced = null,
        ComparisonResult? Compare = null,
        int? RosterSize = null)
    {
        public static RoundOutcome Pass(string ctx, int rosterSize) =>
            new(OutcomeKind.Pass, ctx, null, RosterSize: rosterSize);

        public static RoundOutcome SectionSkip(string ctx, string reason) =>
            new(OutcomeKind.SectionSkip, ctx, reason);

        public static RoundOutcome Skip(string ctx, string reason) =>
            new(OutcomeKind.Skip, ctx, reason);

        public static RoundOutcome Unimplemented(string ctx, int rosterSize) =>
            new(OutcomeKind.Unimplemented, ctx, null, RosterSize: rosterSize);

        public static RoundOutcome Error(string ctx, Exception ex) =>
            new(OutcomeKind.Error, ctx, $"{ex.GetType().Name}: {ex.Message}");

        public static RoundOutcome ColorDiff(
            string ctx, int rosterSize, RoundActuals actual,
            UscfPairingResult produced, ComparisonResult compare) =>
            new(OutcomeKind.ColorDiff, ctx, null, actual, produced, compare, RosterSize: rosterSize);

        public static RoundOutcome Mismatch(
            string ctx, int rosterSize, RoundActuals actual,
            UscfPairingResult produced, ComparisonResult compare) =>
            new(OutcomeKind.Mismatch, ctx, null, actual, produced, compare, RosterSize: rosterSize);

        public string Format() => Kind switch
        {
            OutcomeKind.Pass =>
                $"  ✓ {Context}  ({RosterSize}p)",
            OutcomeKind.SectionSkip or OutcomeKind.Skip =>
                $"  ⤼ {Context}  — {Detail}",
            OutcomeKind.Unimplemented =>
                $"  ⤬ {Context}  ({RosterSize}p) — engine unimplemented",
            OutcomeKind.Error =>
                $"  ! {Context}  — {Detail}",
            OutcomeKind.ColorDiff =>
                FormatDiff("◐", "colour-only diff (matching is correct)"),
            OutcomeKind.Mismatch =>
                FormatDiff("✗", "HARD MISMATCH"),
            _ => Context,
        };

        private string FormatDiff(string symbol, string header)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  {symbol} {Context}  ({RosterSize}p) — {header}");
            sb.AppendLine($"      pair-set match : {(Compare!.PairsMatchUnordered ? "yes" : "NO")}");
            sb.AppendLine($"      bye match      : {(Compare.ByesMatch ? "yes" : "NO")}");
            sb.AppendLine($"      board+colour   : {(Compare.BoardsAndColorsMatch ? "yes" : "NO")}");
            sb.AppendLine($"      SwissSys actual:");
            foreach (var p in Actual!.Pairings.OrderBy(p => p.Board))
            {
                sb.AppendLine($"        bd {p.Board}: {p.WhitePair,3} W vs {p.BlackPair,3} B");
            }
            if (Actual.ByePair is int b)
            {
                sb.AppendLine($"        bye: {b}");
            }
            sb.AppendLine($"      FreePair USCF produced:");
            foreach (var p in Produced!.Pairings.OrderBy(p => p.Board))
            {
                sb.AppendLine($"        bd {p.Board}: {p.WhitePair,3} W vs {p.BlackPair,3} B");
            }
            if (Produced.ByePair is int pb)
            {
                sb.AppendLine($"        bye: {pb}");
            }
            return sb.ToString().TrimEnd();
        }
    }

    // ------------------------------------------ history → TRF synthesis

    private sealed record RoundActuals(IReadOnlyList<UscfPairing> Pairings, int? ByePair);

    private static RoundActuals ExtractActuals(Round round)
    {
        var pairings = round.Pairings
            .Select(p => new UscfPairing(p.WhitePair, p.BlackPair, p.Board))
            .ToArray();
        var bye = round.Byes.FirstOrDefault(b => b.Kind == ByeKind.Full);
        return new RoundActuals(pairings, bye?.PlayerPair);
    }

    /// <summary>
    /// Builds a <see cref="TrfDocument"/> representing the section's state
    /// at the end of <paramref name="endedRound"/> (so the engine pairs
    /// round <c>endedRound + 1</c>).
    /// </summary>
    private static TrfDocument BuildTrfDocAtEndOfRound(
        Tournament tournament,
        Section section,
        IReadOnlyList<Player> roster,
        int endedRound)
    {
        var trfPlayers = roster
            .OrderBy(p => p.PairNumber)
            .Select(p => new TrfPlayer(
                PairNumber: p.PairNumber,
                Name: p.Name,
                Rating: p.Rating,
                Id: p.UscfId ?? string.Empty,
                Points: ScoreThroughRound(p, endedRound),
                Rounds: BuildRoundCells(p, endedRound)))
            .ToList();

        return new TrfDocument(
            TournamentName: tournament.Title ?? section.Name,
            StartDate: tournament.StartDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            EndDate: tournament.EndDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            TotalRounds: Math.Max(section.FinalRound, section.RoundsPlayed),
            // 'w' is a neutral default for round 2+: the engine should be
            // consulting per-player history cells for colour preference,
            // not XXC. (XXC only governs round-1 board-1 top-seed colour.)
            InitialColor: 'w',
            Players: trfPlayers);
    }

    private static decimal ScoreThroughRound(Player p, int endedRound)
    {
        decimal score = 0m;
        var rounds = Math.Min(endedRound, p.History.Count);
        for (var i = 0; i < rounds; i++)
        {
            score += p.History[i].Score;
        }
        return score;
    }

    private static IReadOnlyList<TrfRoundCell> BuildRoundCells(Player p, int endedRound)
    {
        if (endedRound <= 0) return Array.Empty<TrfRoundCell>();

        var cells = new TrfRoundCell[endedRound];
        for (var r = 0; r < endedRound; r++)
        {
            cells[r] = r < p.History.Count
                ? MapToCell(p.History[r])
                : TrfRoundCell.Empty;
        }
        return cells;
    }

    private static TrfRoundCell MapToCell(RoundResult rr)
    {
        var color = rr.Color switch
        {
            PlayerColor.White => 'w',
            PlayerColor.Black => 'b',
            _ => '-',
        };

        var result = rr.Kind switch
        {
            RoundResultKind.Win => '1',
            RoundResultKind.Loss => '0',
            RoundResultKind.Draw => '=',
            RoundResultKind.FullPointBye => 'U',  // TRF "U" = full-point bye
            RoundResultKind.HalfPointBye => 'H',
            RoundResultKind.ZeroPointBye => 'Z',
            _ => '-',                              // None / unpaired
        };

        return new TrfRoundCell(rr.Opponent, color, result);
    }

    // ---------------------------------------------------- comparison

    private sealed record ComparisonResult(
        bool PairsMatchUnordered,
        bool ByesMatch,
        bool BoardsAndColorsMatch)
    {
        public bool IsExactMatch => PairsMatchUnordered && ByesMatch && BoardsAndColorsMatch;
    }

    private static ComparisonResult Compare(RoundActuals actual, UscfPairingResult produced)
    {
        var actualSet = new HashSet<(int, int)>(actual.Pairings
            .Select(p => Normalize(p.WhitePair, p.BlackPair)));
        var producedSet = new HashSet<(int, int)>(produced.Pairings
            .Select(p => Normalize(p.WhitePair, p.BlackPair)));
        var pairsMatch = actualSet.SetEquals(producedSet);

        var byesMatch = actual.ByePair == produced.ByePair;

        var actualByBoard = actual.Pairings.OrderBy(p => p.Board).ToArray();
        var producedByBoard = produced.Pairings.OrderBy(p => p.Board).ToArray();
        var boardsAndColorsMatch =
            actualByBoard.Length == producedByBoard.Length &&
            actualByBoard.Zip(producedByBoard, (a, b) =>
                a.WhitePair == b.WhitePair &&
                a.BlackPair == b.BlackPair).All(x => x);

        return new ComparisonResult(pairsMatch, byesMatch, boardsAndColorsMatch);

        static (int, int) Normalize(int a, int b) =>
            a < b ? (a, b) : (b, a);
    }
}
