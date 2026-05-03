using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FreePair.Core.Tournaments;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;
using Xunit.Abstractions;

namespace FreePair.Core.Tests.Uscf.Harness;

/// <summary>
/// Round-1 verification harness for FreePair's USCF pairing engine
/// (<see cref="UscfPairer"/>). Replays round 1 of every section of every
/// USCF-rated SwissSys tournament under <c>docs/samples/swisssys/uscf/</c>
/// and asserts our pairer reproduces what SwissSys 11 actually produced.
/// </summary>
/// <remarks>
/// <para><b>Why this is a single <c>[Fact]</c> instead of a <c>[Theory]</c>:</b>
/// the corpus might be missing on a fresh clone (the tournaments are
/// real-event PII and may not always be checked in), and we want a
/// rich, copy-pasteable report when sections mismatch — both of which
/// are awkward to get with theory data.</para>
///
/// <para><b>Hard vs soft outcomes.</b> Pair-set and bye correctness are
/// the matching-algorithm invariants — if those are wrong, our engine
/// is buggy and the test fails. Board ordering and colour allocation
/// are <em>soft</em>: a SwissSys-recorded round 1 may differ from a
/// pure algorithmic output because the TD manually swapped colours
/// (rule 29E to break a family/club pairing), or because of 29D
/// equalization rules our phase-0 engine doesn't yet implement.
/// Colour-only diffs are reported but don't fail the test until we
/// reach P3.</para>
///
/// <para><b>Initial color (XXC) inference:</b> we don't know which colour
/// SwissSys gave the top seed of board 1, so the harness runs the pairer
/// twice — once with <c>white1</c>, once with <c>black1</c> — and reports
/// whichever matches better. The picker prefers whichever gives the
/// strongest comparison overall.</para>
///
/// <para><b>What's skipped right now:</b> sections with any pre-flagged
/// half-point bye in round 1 (P4 territory), withdrawals before round 1,
/// or zero players. Skips are reported but don't fail the test.</para>
/// </remarks>
public class UscfRound1HarnessTests
{
    private readonly ITestOutputHelper _output;

    public UscfRound1HarnessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Pinned baseline counts from the donated corpus as of P5a expansion.
    /// The test fails when:
    /// <list type="bullet">
    ///   <item><c>matched</c> drops below <see cref="MinExpectedMatches"/>
    ///         (algorithm regression — a section that used to match
    ///         doesn't any more);</item>
    ///   <item><c>hard mismatch</c> exceeds <see cref="MaxExpectedHardMismatches"/>
    ///         (a new section started failing — either a regression or
    ///         freshly-added corpus revealing a new divergence);</item>
    ///   <item><c>colour-only diff</c> exceeds <see cref="MaxExpectedColorOnlyDiffs"/>
    ///         (same reasoning, on the colour-allocation axis);</item>
    ///   <item>any case threw an exception other than the deliberately-
    ///         caught loader failures.</item>
    /// </list>
    /// When the engine improves and these numbers drift in the good
    /// direction (matches up, mismatches down), bump the constants so the
    /// new bar becomes the regression floor.
    /// </summary>
    private const int MinExpectedMatches         = 112;
    private const int MaxExpectedHardMismatches  = 27;
    private const int MaxExpectedColorOnlyDiffs  = 6;

    [Fact]
    public async System.Threading.Tasks.Task Round1_pairings_match_swisssys_for_every_uscf_sample()
    {
        var files = UscfSampleDiscovery.FinalStateFiles();

        if (files.Count == 0)
        {
            // No corpus available on this machine — that's fine, the corpus
            // is gitignored in some setups for PII reasons. Surface it as a
            // visible note rather than a silent pass.
            _output.WriteLine(
                "(no USCF samples found under docs/samples/swisssys/uscf/ — harness is a no-op)");
            return;
        }

        var loader = new TournamentLoader();
        var report = new HarnessReport();

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
                report.Process(file, tournament, section);
            }
        }

        _output.WriteLine(report.Render());

        var problems = new List<string>();

        if (report.MatchedCount < MinExpectedMatches)
        {
            problems.Add($"matched count regressed: {report.MatchedCount} < expected ≥ {MinExpectedMatches}. " +
                         "Some section that used to match SwissSys now doesn't — investigate before merging.");
        }
        if (report.HardFailures > MaxExpectedHardMismatches)
        {
            problems.Add($"hard mismatches grew: {report.HardFailures} > expected ≤ {MaxExpectedHardMismatches}. " +
                         "Either a new sample was added that exposes a divergence (bump the constant), " +
                         "or a previously-matching section started failing (fix the regression).");
        }
        if (report.ColorOnlyDiffs > MaxExpectedColorOnlyDiffs)
        {
            problems.Add($"colour-only diffs grew: {report.ColorOnlyDiffs} > expected ≤ {MaxExpectedColorOnlyDiffs}. " +
                         "Same diagnosis as hard mismatches — investigate or bump the bar.");
        }
        if (report.ErrorCount > 0)
        {
            problems.Add($"the engine threw {report.ErrorCount} unexpected exception(s) — see report.");
        }

        if (problems.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                "USCF round-1 harness regression(s) detected:" + Environment.NewLine +
                "  - " + string.Join(Environment.NewLine + "  - ", problems) +
                Environment.NewLine + "(see test output for the full breakdown)");
        }
    }

    // ---------------------------------------------------------------- report

    private sealed class HarnessReport
    {
        private readonly List<SectionOutcome> _outcomes = new();
        private readonly List<string> _loadFailures = new();

        public int HardFailures => _outcomes.Count(o => o.Kind == OutcomeKind.Mismatch);
        public int MatchedCount => _outcomes.Count(o => o.Kind == OutcomeKind.Pass);
        public int ColorOnlyDiffs => _outcomes.Count(o => o.Kind == OutcomeKind.ColorDiff);
        public int ErrorCount => _outcomes.Count(o => o.Kind == OutcomeKind.Error);

        public void RecordLoadFailure(string file, Exception ex) =>
            _loadFailures.Add($"{Rel(file)}: {ex.GetType().Name}: {ex.Message}");

        public void Process(string file, Tournament tournament, Section section)
        {
            var ctx = $"{Rel(file)} :: \"{section.Name}\"";

            if (section.SoftDeleted)
            {
                _outcomes.Add(SectionOutcome.Skip(ctx, "soft-deleted section"));
                return;
            }
            if (section.RoundsPlayed < 1)
            {
                _outcomes.Add(SectionOutcome.Skip(ctx, "round 1 not yet played"));
                return;
            }
            if (section.Players.Count == 0)
            {
                _outcomes.Add(SectionOutcome.Skip(ctx, "empty roster"));
                return;
            }
            if (section.Rounds.Count < 1)
            {
                _outcomes.Add(SectionOutcome.Skip(ctx,
                    "section has RoundsPlayed >= 1 but no Rounds[] entries (mapper issue?)"));
                return;
            }

            // Extract what SwissSys actually did in round 1.
            var actual = ExtractRound1Actuals(section);

            // The "round-1 pool" is exactly the union of {paired in round 1}
            // ∪ {got a bye in round 1}. Anyone in section.Players outside
            // that set wasn't actually present at round 1 — late entry,
            // withdrawn pre-round-1, or some other TD-side exclusion the
            // mapper doesn't surface. Feeding our engine those phantom
            // players guarantees a mismatch (different field size, wrong
            // bye assignment), so build the roster from the actuals.
            var actualPool = new HashSet<int>();
            foreach (var p in actual.Pairings)
            {
                actualPool.Add(p.WhitePair);
                actualPool.Add(p.BlackPair);
            }
            if (actual.ByePair is int byePair)
            {
                actualPool.Add(byePair);
            }

            var roster = section.Players
                .Where(p => !p.SoftDeleted && actualPool.Contains(p.PairNumber))
                .ToArray();

            // Anyone in the round-1 pool who wasn't in section.Players is
            // a data anomaly worth surfacing (mapper drops them, but
            // SwissSys had them paired) — should never happen in practice.
            var ghosts = actualPool
                .Where(pair => !section.Players.Any(p => p.PairNumber == pair))
                .ToArray();
            if (ghosts.Length > 0)
            {
                _outcomes.Add(SectionOutcome.Skip(ctx,
                    $"round 1 references {ghosts.Length} pair number(s) not in section roster: " +
                    string.Join(",", ghosts)));
                return;
            }

            if (roster.Length == 0)
            {
                _outcomes.Add(SectionOutcome.Skip(ctx,
                    "round-1 pool is empty (mapper produced no playable players)"));
                return;
            }

            // Anyone with a half-point bye request for round 1, or with
            // round-1 already in their zero-point bye set, is special-
            // cased by TrfWriter (and by SwissSys in subtly different
            // ways). Skip those for the round-1 skeleton harness — P4
            // will handle them.
            var byeQuirks = roster
                .Where(p => p.RequestedByeRounds.Contains(1) ||
                            p.ZeroPointByeRoundsOrEmpty.Contains(1))
                .ToArray();
            if (byeQuirks.Length > 0)
            {
                _outcomes.Add(SectionOutcome.Skip(ctx,
                    $"round 1 has {byeQuirks.Length} pre-flagged bye(s) — gated until P4"));
                return;
            }

            // Run our pairer with both possible initial colours; pick
            // the one that matches the actual pairings best.
            var trfWhite = BuildPreRound1TrfDoc(tournament, section, roster, initialColor: 'w');
            var trfBlack = BuildPreRound1TrfDoc(tournament, section, roster, initialColor: 'b');

            UscfPairingResult? produced;
            try
            {
                var rWhite = UscfPairer.Pair(trfWhite);
                var rBlack = UscfPairer.Pair(trfBlack);

                produced = PickBetterMatch(actual, rWhite, rBlack, out var inferredColor);

                var match = Compare(actual, produced);

                if (match.IsExactMatch)
                {
                    _outcomes.Add(SectionOutcome.Pass(ctx, roster.Length, inferredColor));
                }
                else if (match.PairsMatchUnordered && match.ByesMatch)
                {
                    // Pair-set + bye are right (the matching invariant).
                    // Only colour / board ordering differ — likely a TD
                    // manual swap or rule-29D equalization our P0 engine
                    // doesn't model yet. Report but don't fail the test.
                    _outcomes.Add(SectionOutcome.ColorDiff(ctx, roster.Length, inferredColor, actual, produced, match));
                }
                else
                {
                    _outcomes.Add(SectionOutcome.Mismatch(ctx, roster.Length, inferredColor, actual, produced, match));
                }
            }
            catch (Exception ex)
            {
                _outcomes.Add(SectionOutcome.Error(ctx, ex));
            }
        }

        public string Render()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("===== USCF round-1 harness =====");

            var passes = _outcomes.Count(o => o.Kind == OutcomeKind.Pass);
            var skips = _outcomes.Count(o => o.Kind == OutcomeKind.Skip);
            var colorDiffs = _outcomes.Count(o => o.Kind == OutcomeKind.ColorDiff);
            var fails = _outcomes.Count(o => o.Kind == OutcomeKind.Mismatch);
            var errors = _outcomes.Count(o => o.Kind == OutcomeKind.Error);

            sb.AppendLine($"Sections seen : {_outcomes.Count}");
            sb.AppendLine($"  ✓ matched     : {passes}");
            sb.AppendLine($"  ⤼ skipped     : {skips}");
            sb.AppendLine($"  ◐ colour-only : {colorDiffs}  (matching is correct; colour allocation differs — gated until P3)");
            sb.AppendLine($"  ✗ mismatch    : {fails}");
            sb.AppendLine($"  ! error       : {errors}");
            if (_loadFailures.Count > 0)
            {
                sb.AppendLine($"Load failures : {_loadFailures.Count}");
            }
            sb.AppendLine();

            foreach (var o in _outcomes)
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

    // ------------------------------------------------ section outcome record

    private enum OutcomeKind { Pass, Skip, ColorDiff, Mismatch, Error }

    private sealed record SectionOutcome(
        OutcomeKind Kind,
        string Context,
        string? Detail,
        Round1Actuals? Actual = null,
        UscfPairingResult? Produced = null,
        ComparisonResult? Compare = null,
        int? RosterSize = null,
        char? InferredInitialColor = null)
    {
        public static SectionOutcome Pass(string ctx, int rosterSize, char inferred) =>
            new(OutcomeKind.Pass, ctx, null, RosterSize: rosterSize, InferredInitialColor: inferred);

        public static SectionOutcome Skip(string ctx, string reason) =>
            new(OutcomeKind.Skip, ctx, reason);

        public static SectionOutcome Error(string ctx, Exception ex) =>
            new(OutcomeKind.Error, ctx, $"{ex.GetType().Name}: {ex.Message}");

        public static SectionOutcome ColorDiff(
            string ctx, int rosterSize, char inferred,
            Round1Actuals actual, UscfPairingResult produced, ComparisonResult compare) =>
            new(OutcomeKind.ColorDiff, ctx, null, actual, produced, compare,
                RosterSize: rosterSize, InferredInitialColor: inferred);

        public static SectionOutcome Mismatch(
            string ctx, int rosterSize, char inferred,
            Round1Actuals actual, UscfPairingResult produced, ComparisonResult compare) =>
            new(OutcomeKind.Mismatch, ctx, null, actual, produced, compare,
                RosterSize: rosterSize, InferredInitialColor: inferred);

        public string Format() => Kind switch
        {
            OutcomeKind.Pass =>
                $"  ✓ {Context}  ({RosterSize}p, init={InferredInitialColor})",
            OutcomeKind.Skip =>
                $"  ⤼ {Context}  — {Detail}",
            OutcomeKind.Error =>
                $"  ! {Context}  — {Detail}",
            OutcomeKind.ColorDiff =>
                FormatDiff(symbol: "◐", header: "colour-only diff (matching is correct)"),
            OutcomeKind.Mismatch =>
                FormatDiff(symbol: "✗", header: "HARD MISMATCH"),
            _ => Context,
        };

        private string FormatDiff(string symbol, string header)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  {symbol} {Context}  ({RosterSize}p, init={InferredInitialColor}) — {header}");
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

    // ----------------------------------------------- comparison + inference

    private sealed record Round1Actuals(IReadOnlyList<UscfPairing> Pairings, int? ByePair);

    private static Round1Actuals ExtractRound1Actuals(Section section)
    {
        var round1 = section.Rounds.FirstOrDefault(r => r.Number == 1)
                  ?? section.Rounds[0];

        var pairings = round1.Pairings
            .Select(p => new UscfPairing(p.WhitePair, p.BlackPair, p.Board))
            .ToArray();

        // SwissSys records the round-1 full-point bye as a ByeAssignment
        // with kind Full. There's at most one in round 1 (odd field).
        var bye = round1.Byes.FirstOrDefault(b => b.Kind == ByeKind.Full);
        return new Round1Actuals(pairings, bye?.PlayerPair);
    }

    private static TrfDocument BuildPreRound1TrfDoc(
        Tournament tournament,
        Section section,
        IReadOnlyList<Player> roster,
        char initialColor)
    {
        var trfPlayers = roster
            .OrderBy(p => p.PairNumber)
            .Select(p => new TrfPlayer(
                PairNumber: p.PairNumber,
                Name: p.Name,
                Rating: p.Rating,
                Id: p.UscfId ?? string.Empty,
                Points: 0m,
                Rounds: Array.Empty<TrfRoundCell>()))
            .ToList();

        return new TrfDocument(
            TournamentName: tournament.Title ?? section.Name,
            StartDate: tournament.StartDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            EndDate: tournament.EndDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            TotalRounds: Math.Max(section.FinalRound, section.RoundsPlayed),
            InitialColor: initialColor,
            Players: trfPlayers);
    }

    private static UscfPairingResult PickBetterMatch(
        Round1Actuals actual,
        UscfPairingResult white,
        UscfPairingResult black,
        out char inferredColor)
    {
        var whiteCmp = Compare(actual, white);
        var blackCmp = Compare(actual, black);

        // Prefer the result that fully matches; if neither does, prefer the
        // one with a matching pair-set (since color-only mismatches should
        // be reported against that scenario, not the other).
        if (whiteCmp.IsExactMatch && !blackCmp.IsExactMatch)
        {
            inferredColor = 'w';
            return white;
        }
        if (blackCmp.IsExactMatch && !whiteCmp.IsExactMatch)
        {
            inferredColor = 'b';
            return black;
        }
        if (whiteCmp.IsExactMatch && blackCmp.IsExactMatch)
        {
            // Symmetric case (1- or 2-player edge cases). Pick white.
            inferredColor = 'w';
            return white;
        }
        // Neither is exact. Score each and pick the better.
        if (Score(whiteCmp) >= Score(blackCmp))
        {
            inferredColor = 'w';
            return white;
        }
        inferredColor = 'b';
        return black;

        static int Score(ComparisonResult c) =>
            (c.PairsMatchUnordered ? 4 : 0) +
            (c.ByesMatch ? 2 : 0) +
            (c.BoardsAndColorsMatch ? 1 : 0);
    }

    private sealed record ComparisonResult(
        bool PairsMatchUnordered,
        bool ByesMatch,
        bool BoardsAndColorsMatch)
    {
        public bool IsExactMatch => PairsMatchUnordered && ByesMatch && BoardsAndColorsMatch;
    }

    private static ComparisonResult Compare(Round1Actuals actual, UscfPairingResult produced)
    {
        // Unordered pair-set: SwissSys says "9 vs 1", we say "9 vs 1" or
        // "1 vs 9" — both represent the same matchup.
        var actualSet = new HashSet<(int, int)>(actual.Pairings
            .Select(p => Normalize(p.WhitePair, p.BlackPair)));
        var producedSet = new HashSet<(int, int)>(produced.Pairings
            .Select(p => Normalize(p.WhitePair, p.BlackPair)));
        var pairsMatch = actualSet.SetEquals(producedSet);

        var byesMatch = actual.ByePair == produced.ByePair;

        // Board+colour: compare positionally after sorting by board.
        // We don't compare board numbers directly because SwissSys honours
        // the section's FirstBoard offset (bd 31..38 for Apr 2026 Open I)
        // while our engine always emits 1..N. Position-by-position is the
        // right invariant: "same colour assignment in the same slot order".
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
