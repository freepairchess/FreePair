using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;
using Xunit.Abstractions;

namespace FreePair.Core.Tests.Uscf.Harness;

/// <summary>
/// Round-by-round comparison harness for the played-state version of
/// the 90th Greater Boston Open, supplied by the user as an NA Chess
/// Hub publishing-format JSON file (different schema from SwissSys's
/// native .sjson — players carry parallel <c>ops</c> / <c>colors</c> /
/// <c>results</c> / <c>boards</c> arrays of length = rounds played,
/// from which the actual round pairings are reconstructed).
/// </summary>
/// <remarks>
/// <para>For each section in the file, replays rounds 2 through
/// <c>rounds_played</c> and checks whether our USCF engine produces
/// the same pair set SwissSys did. Purely informational — never
/// fails. Surfaces per-round pair-set match/mismatch with a per-
/// section summary so the algorithmic gap on a real, large, mostly-
/// open event is visible alongside the MCC and Puddletown
/// diagnostics.</para>
///
/// <para>The 90th GBO is the largest single event in the corpus
/// once played: 8 sections, 200 players (Premier 22, Under 2200 17,
/// Under 2000 26, Under 1800 24, Under 1600 24, Under 1400 21,
/// Under 1200 20, Under 1000 38, Side Games 8). Four rounds. Almost
/// no team-tagging — most divergence is pure score-group / float /
/// transposition algorithmic difference.</para>
/// </remarks>
public class UscfNachPlayedStateComparisonTests
{
    /// <summary>
    /// Pinned baselines captured after the post-cleanup NACH corpus
    /// landed (56 tournaments, RR / norm / pre-event noise removed).
    /// The aggregate test fails when either:
    /// <list type="bullet">
    ///   <item><c>matchedRounds</c> drops below
    ///         <see cref="MinExpectedMatchedRounds"/> — a previously-
    ///         matching round/section started failing;</item>
    ///   <item><c>matchedPairs</c> drops below
    ///         <see cref="MinExpectedMatchedPairs"/> — same diagnosis
    ///         at the individual-pair granularity.</item>
    /// </list>
    /// <para>NACH is the user's preferred truth source over the older
    /// .sjson corpus because it captures every played round verbatim
    /// (each player's <c>ops</c> / <c>colors</c> / <c>results</c>
    /// arrays are direct from the SwissSys publishing export, with no
    /// schema lossiness). Any algorithmic engine change that improves
    /// either count above the baseline can ratchet these constants
    /// upward in the same commit; a regression that drops either
    /// below the baseline fails the test.</para>
    ///
    /// <para><b>2026-05 ratchet — USCF 28F2 floater placement</b>
    /// (UscfPairer.MergeWithFloaters): downfloaters now go to the
    /// TOP of the BOTTOM HALF of the combined pool so SLIDE pairs
    /// them with the highest-rated of the lower score group. Effect
    /// on this corpus:</para>
    /// <list type="bullet">
    ///   <item>matched rounds 61 → 98 (+37)</item>
    ///   <item>matched pairs 2269 → 2995 (+726)</item>
    /// </list>
    /// </summary>
    private const int MinExpectedMatchedRounds = 98;
    private const int MinExpectedMatchedPairs  = 2995;

    private readonly ITestOutputHelper _output;

    public UscfNachPlayedStateComparisonTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Aggregate sweep across the entire NACH played-state corpus.
    /// Walks every <c>docs/samples/nach/*.json</c>, runs the engine on
    /// every section's rounds 2..N, prints a per-tournament + grand-
    /// total summary table to the test output, and asserts the
    /// matched-rounds / matched-pairs counts haven't regressed below
    /// the pinned baselines.
    /// </summary>
    [Fact]
    public void All_NACH_tournaments_aggregate_pair_set_comparison()
    {
        var nachDir = Path.Combine(TestPaths.RepoRoot, "docs", "samples", "nach");
        if (!Directory.Exists(nachDir))
        {
            _output.WriteLine($"(no NACH corpus at {nachDir} — diagnostic is a no-op)");
            return;
        }

        var files = Directory.EnumerateFiles(nachDir, "*.json")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0)
        {
            _output.WriteLine($"(NACH corpus at {nachDir} is empty)");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"===== NACH played-state aggregate ({files.Count} tournaments) =====");
        sb.AppendLine();
        sb.AppendLine("                                                           rounds        individual pairs");
        sb.AppendLine("tournament                                                match  total    match  total   pct");
        sb.AppendLine(new string('-', 100));

        var totalRounds = 0;
        var matchedRounds = 0;
        var totalPairs = 0;
        var matchedPairs = 0;
        var loadFailures = 0;

        foreach (var file in files)
        {
            var (rounds, matchedR, pairs, matchedP, loaded) = ScanFile(file);
            if (!loaded) { loadFailures++; continue; }

            var label = Path.GetFileNameWithoutExtension(file);
            if (label.Length > 56) label = label.Substring(0, 53) + "...";
            sb.AppendLine($"  {label,-56}  {matchedR,4} / {rounds,4}    {matchedP,4} / {pairs,4}   {Pct(matchedP, pairs),5}");

            totalRounds += rounds;
            matchedRounds += matchedR;
            totalPairs += pairs;
            matchedPairs += matchedP;
        }

        sb.AppendLine(new string('-', 100));
        sb.AppendLine($"  {"TOTALS",-56}  {matchedRounds,4} / {totalRounds,4}    {matchedPairs,4} / {totalPairs,4}   {Pct(matchedPairs, totalPairs),5}");
        if (loadFailures > 0) sb.AppendLine($"  load failures: {loadFailures}");
        sb.AppendLine();
        sb.AppendLine($"baselines: matched rounds ≥ {MinExpectedMatchedRounds}, matched pairs ≥ {MinExpectedMatchedPairs}");

        _output.WriteLine(sb.ToString());

        // Pinned regression guard. Improvements ratchet these
        // constants upward in the same commit; regressions fail the
        // test so a future engine change that drops the corpus match
        // rate below baseline is caught immediately.
        var problems = new List<string>();
        if (matchedRounds < MinExpectedMatchedRounds)
        {
            problems.Add($"matched-rounds regressed: {matchedRounds} < expected ≥ {MinExpectedMatchedRounds}");
        }
        if (matchedPairs < MinExpectedMatchedPairs)
        {
            problems.Add($"matched-pairs regressed: {matchedPairs} < expected ≥ {MinExpectedMatchedPairs}");
        }
        if (problems.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                "NACH aggregate regression(s):" + Environment.NewLine +
                "  - " + string.Join(Environment.NewLine + "  - ", problems) +
                Environment.NewLine + "(see test output for the full per-tournament breakdown)");
        }
    }

    /// <summary>Detailed per-round dump for a specific tournament.</summary>
    [Fact]
    public void Greater_Boston_Open_90th_round_by_round_pair_set_comparison()
    {
        RunComparison("90th_Greater_Boston_Open.json");
    }

    /// <summary>Detailed per-round dump for a specific tournament.</summary>
    [Fact]
    public void Massachusetts_Senior_Open_9th_round_by_round_pair_set_comparison()
    {
        RunComparison("9th_Massachusetts_Senior_Open.json");
    }

    /// <summary>
    /// User-reported bug repro (TestUSCFPair sjson dated 2026-04-25):
    /// for the 9th MA Senior Open Open section, after R1 played the
    /// engine should give the full-point bye in R2 to the lowest-rated
    /// player in the lowest score group. The score-0 group has 7
    /// players (Terrie 2204, Barnakov 2158, Carnevale 1898, Dame 1871,
    /// Gradijan 1821, Urbonas 1794, Smith 1778); Smith is the lowest
    /// rated and SwissSys correctly assigns him the full-point bye.
    /// FreePair was assigning Urbonas instead — this test captures
    /// the actual <c>UscfPairer.ByePair</c> output and asserts it is
    /// Smith (pair 21), not Urbonas (pair 20).
    /// </summary>
    [Fact]
    public void Senior_Open_R2_full_point_bye_goes_to_lowest_rated_in_lowest_score_group()
    {
        var path = Path.Combine(TestPaths.RepoRoot,
            "docs", "samples", "nach", "9th_Massachusetts_Senior_Open.json");
        if (!File.Exists(path)) { _output.WriteLine("(NACH fixture not present)"); return; }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var open = root.GetProperty("sections").EnumerateArray()
            .First(s => s.GetProperty("section").GetString() == "Open");

        // Build NachPlayer list, then run the same compose-TRF-and-Pair
        // pipeline the aggregate harness uses, but for this specific
        // (section, round) only.
        var players = new List<NachPlayer>();
        var pairNo = 1;
        foreach (var pe in open.GetProperty("players").EnumerateArray())
        {
            players.Add(NachPlayer.From(pe, pairNo));
            pairNo++;
        }

        const int round = 2;
        var rosterPairs = new HashSet<int>();
        foreach (var p in players)
        {
            if (round - 1 >= p.Ops.Count) continue;
            var opp = p.Ops[round - 1];
            if (opp != 0) { rosterPairs.Add(p.PairNumber); rosterPairs.Add(opp); }
        }
        foreach (var p in players)
        {
            if (round - 1 >= p.Results.Count) continue;
            var res = p.Results[round - 1];
            if (res == 'H' || res == 'B') rosterPairs.Add(p.PairNumber);
        }
        var requestedByes = new Dictionary<int, char>();
        foreach (var p in players)
        {
            if (round - 1 >= p.Results.Count) continue;
            if (!rosterPairs.Contains(p.PairNumber)) continue;
            if (p.Results[round - 1] == 'H') requestedByes[p.PairNumber] = 'H';
        }
        var roster = players.Where(p => rosterPairs.Contains(p.PairNumber))
                            .OrderBy(p => p.PairNumber).ToList();

        var trf = BuildTrfDoc(roster, endedRound: round - 1, "9th MA Senior Open", "2026-04-25")
            with { RequestedByes = requestedByes.Count == 0 ? null : requestedByes };
        var produced = UscfPairer.Pair(trf);

        // Diagnostic print so the test output makes the failure
        // obvious if the engine drifts again.
        _output.WriteLine($"engine ByePair = {produced.ByePair}");
        _output.WriteLine($"engine pairings:");
        foreach (var pp in produced.Pairings.OrderBy(x => x.Board))
        {
            var w = roster.FirstOrDefault(r => r.PairNumber == pp.WhitePair);
            var b = roster.FirstOrDefault(r => r.PairNumber == pp.BlackPair);
            _output.WriteLine($"  bd{pp.Board,2}  W#{pp.WhitePair,-2} {w?.Name,-25} (rt {w?.Rating,4}) vs  B#{pp.BlackPair,-2} {b?.Name,-25} (rt {b?.Rating,4})");
        }

        // Dump the harness's TrfDocument so it can be diffed against
        // the live-app pipeline diagnostic to find the divergence.
        _output.WriteLine($"harness TrfDocument: TotalRounds={trf.TotalRounds}, InitialColor={trf.InitialColor}");
        _output.WriteLine($"harness RequestedByes=" +
            (trf.RequestedByes is null ? "null"
                : "{" + string.Join(",", trf.RequestedByes.Select(kv => $"{kv.Key}={kv.Value}")) + "}"));
        foreach (var p in trf.Players.OrderBy(x => x.PairNumber))
        {
            var cells = string.Join(" ",
                p.Rounds.Select(c => $"({c.Opponent},{c.Color},{c.Result})"));
            _output.WriteLine($"  #{p.PairNumber,-2} {p.Name,-28} rt={p.Rating,4} pts={p.Points,4}  [{cells}]");
        }

        // Assertion: lowest-rated of the lowest score group (score 0)
        // gets the full-point bye. That's Smith, pair 21. Urbonas
        // (pair 20) is one rating point higher and should NOT be the
        // bye recipient.
        Assert.Equal(21, produced.ByePair);
    }

    /// <summary>
    /// End-to-end pipeline diagnostic for the same Senior Open R2
    /// scenario, but driven through the EXACT path the live app
    /// uses: load the user's <c>.sjson</c> via
    /// <see cref="SwissSysImporter"/>, truncate any already-paired
    /// R2, render TRF text via <see cref="TrfWriter"/> (with
    /// <c>pairingRound=2</c> so half-bye pre-flag cells are emitted
    /// just like <c>BbpPairingEngine.GenerateNextRoundAsync</c>
    /// does), parse it back via <see cref="TrfReader.Parse"/>, and
    /// run <see cref="UscfPairer.Pair"/> on the result.
    ///
    /// <para>Skips silently when the user's external <c>.sjson</c>
    /// fixture isn't present (it lives in
    /// <c>~\OneDrive\Desktop\TestUSCFPair</c> on the dev box, not
    /// in-repo). The test is informational only — it dumps the
    /// engine's pairings + the TRF cells the engine actually saw to
    /// the test-output channel so the live-app vs harness
    /// divergence can be traced when it shows up.</para>
    /// </summary>
    [Fact]
    public async Task Senior_Open_R2_live_app_pipeline_diagnostic()
    {
        var path = @"C:\Users\xuhaohe\OneDrive - Microsoft\Desktop\TestUSCFPair\9th_Massachusetts_Senior_Open.sjson";
        if (!File.Exists(path))
        {
            _output.WriteLine($"(external .sjson fixture not present at {path} -- diagnostic is a no-op)");
            return;
        }

        var loader = new FreePair.Core.Tournaments.TournamentLoader();
        var t = await loader.LoadAsync(path);

        // Truncate any already-paired R2 so we re-pair fresh from
        // the post-R1 state.
        while (t.Sections.First(s => s.Name == "Open").Rounds.Count >= 2)
        {
            t = FreePair.Core.Tournaments.TournamentMutations.DeleteLastRound(t, "Open");
        }
        var openSec = t.Sections.First(s => s.Name == "Open");
        _output.WriteLine($"Open section: {openSec.Players.Count} players, {openSec.Rounds.Count} round(s) of history");

        // Generate TRF the way the live app does for R2 pairing:
        // pairingRound=2 so half-bye pre-flag cells are emitted for
        // any player with R2 in their RequestedByeRounds.
        var trfText = FreePair.Core.Trf.TrfWriter.Write(
            t, openSec,
            FreePair.Core.Bbp.InitialColor.White,
            pairingRound: 2);

        // Round-trip via the same TrfReader the FreePair.UscfEngine
        // binary uses, so we feed UscfPairer EXACTLY the document
        // it gets in production.
        var doc = FreePair.Core.Uscf.Trf.TrfReader.Parse(trfText);
        _output.WriteLine($"Parsed: {doc.Players.Count} players, RequestedByes=" +
            (doc.RequestedByes is null ? "null"
                : "{" + string.Join(",", doc.RequestedByes.Select(kv => $"{kv.Key}={kv.Value}")) + "}"));

        var produced = FreePair.Core.Uscf.UscfPairer.Pair(doc);
        _output.WriteLine($"engine ByePair = {produced.ByePair}");
        _output.WriteLine($"engine pairings:");
        foreach (var pp in produced.Pairings.OrderBy(x => x.Board))
        {
            var w = doc.Players.First(x => x.PairNumber == pp.WhitePair);
            var b = doc.Players.First(x => x.PairNumber == pp.BlackPair);
            _output.WriteLine($"  bd{pp.Board,2}  W#{pp.WhitePair,-2} {w.Name,-28} (rt {w.Rating,4})  vs  B#{pp.BlackPair,-2} {b.Name,-28} (rt {b.Rating,4})");
        }

        // Dump per-player score + history cell summary so any
        // discrepancy with the harness's BuildTrfDoc ordering /
        // cell content is visible at a glance.
        _output.WriteLine($"player TRF state (#pair  rating  score  history-cells):");
        foreach (var p in doc.Players.OrderBy(x => x.PairNumber))
        {
            var cells = string.Join(" ",
                p.Rounds.Select(c => $"({c.Opponent},{c.Color},{c.Result})"));
            _output.WriteLine($"  #{p.PairNumber,-2} {p.Name,-28} rt={p.Rating,4} pts={p.Points,4}  [{cells}]");
        }

        // Pinned: the bye must still be Smith (pair 21). Beyond
        // that, the test is purely informational so engine drift
        // surfaces in the test output without a hard failure.
        Assert.Equal(21, produced.ByePair);
    }

    /// <summary>
    /// Lightweight pass over a single NACH file used by the aggregate
    /// sweep — same logic as <see cref="RunComparison"/> but doesn't
    /// emit per-round detail and short-circuits on file errors.
    /// </summary>
    private static (int rounds, int matchedRounds, int pairs, int matchedPairs, bool loaded)
        ScanFile(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var tournamentName = root.GetProperty("tournament").GetString() ?? "";
            var date = root.TryGetProperty("date", out var dp) ? dp.GetString() ?? "" : "";

            var rounds = 0; var matchedRounds = 0; var pairs = 0; var matchedPairs = 0;

            foreach (var sectionElem in root.GetProperty("sections").EnumerateArray())
            {
                var roundsPlayed = sectionElem.TryGetProperty("rounds_played", out var rp)
                    && rp.ValueKind == JsonValueKind.Number ? rp.GetInt32() : 0;
                if (roundsPlayed < 2) continue;

                var players = new List<NachPlayer>();
                var pairNo = 1;
                foreach (var pe in sectionElem.GetProperty("players").EnumerateArray())
                {
                    players.Add(NachPlayer.From(pe, pairNo));
                    pairNo++;
                }

                var sink = new StringBuilder();   // throwaway -- aggregate doesn't print details
                for (var round = 2; round <= roundsPlayed; round++)
                {
                    var (caseSeen, matched, actualCount, matchedCount) =
                        CompareRound(sink, players, round, tournamentName, date);
                    if (!caseSeen) continue;
                    rounds++;
                    if (matched) matchedRounds++;
                    pairs += actualCount;
                    matchedPairs += matchedCount;
                }
            }

            return (rounds, matchedRounds, pairs, matchedPairs, true);
        }
        catch
        {
            return (0, 0, 0, 0, false);
        }
    }

    /// <summary>
    /// Loads a NACH-format played-state JSON file and walks every
    /// section's rounds 2..N comparing actual SwissSys-produced pair
    /// sets against <see cref="UscfPairer"/>'s output. Pure
    /// informational dump — never fails. The diagnostic output is
    /// written to the xunit test output channel so the IDE / CLI
    /// surfaces it on demand.
    /// </summary>
    private void RunComparison(string nachFileName)
    {
        var path = Path.Combine(TestPaths.RepoRoot, "docs", "samples", "nach", nachFileName);
        if (!File.Exists(path))
        {
            _output.WriteLine($"(NACH-format played-state file not found at {path} — diagnostic is a no-op)");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var tournamentName = root.GetProperty("tournament").GetString() ?? "(unknown)";
        var date = root.TryGetProperty("date", out var dateProp) ? dateProp.GetString() ?? "" : "";

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"===== {tournamentName} ({date}) :: round-by-round comparison =====");
        sb.AppendLine();

        var totalRounds = 0;
        var matchedRounds = 0;
        var totalPairs = 0;
        var matchedPairs = 0;

        foreach (var sectionElem in root.GetProperty("sections").EnumerateArray())
        {
            var sectionName = sectionElem.GetProperty("section").GetString() ?? "(unnamed)";
            var roundsPlayed = sectionElem.TryGetProperty("rounds_played", out var rp) && rp.ValueKind == JsonValueKind.Number
                ? rp.GetInt32()
                : 0;
            if (roundsPlayed < 2)
            {
                sb.AppendLine($"### [{sectionName}] — only {roundsPlayed} rounds played, no R2+ to compare");
                sb.AppendLine();
                continue;
            }

            // Build the section's player list. Pair number = 1-indexed
            // position in the players array.
            var players = new List<NachPlayer>();
            var playerArr = sectionElem.GetProperty("players");
            var pairNo = 1;
            foreach (var pe in playerArr.EnumerateArray())
            {
                players.Add(NachPlayer.From(pe, pairNo));
                pairNo++;
            }

            sb.AppendLine($"### [{sectionName}]  ({players.Count} players, {roundsPlayed} rounds played)");

            var sectionMatchedRounds = 0;
            var sectionTotalRounds = 0;
            var sectionMatchedPairs = 0;
            var sectionTotalPairs = 0;

            for (var round = 2; round <= roundsPlayed; round++)
            {
                var (caseSeen, matched, actualCount, matchedCount) =
                    CompareRound(sb, players, round, tournamentName, date);
                if (caseSeen)
                {
                    sectionTotalRounds++;
                    if (matched) sectionMatchedRounds++;
                    sectionTotalPairs += actualCount;
                    sectionMatchedPairs += matchedCount;
                }
            }

            sb.AppendLine($"  section subtotal:  rounds {sectionMatchedRounds}/{sectionTotalRounds}  ·  individual pairs {sectionMatchedPairs}/{sectionTotalPairs}");
            sb.AppendLine();

            totalRounds += sectionTotalRounds;
            matchedRounds += sectionMatchedRounds;
            totalPairs += sectionTotalPairs;
            matchedPairs += sectionMatchedPairs;
        }

        sb.AppendLine($"---- summary ----");
        sb.AppendLine($"comparable rounds   : {totalRounds}");
        sb.AppendLine($"  pair-set matches  : {matchedRounds}  ({Pct(matchedRounds, totalRounds)})");
        sb.AppendLine($"  pair-set diffs    : {totalRounds - matchedRounds}");
        sb.AppendLine();
        sb.AppendLine($"individual pairs    : {totalPairs}");
        sb.AppendLine($"  matched           : {matchedPairs}  ({Pct(matchedPairs, totalPairs)})");
        sb.AppendLine($"  diverged          : {totalPairs - matchedPairs}");

        _output.WriteLine(sb.ToString());
    }

    /// <summary>
    /// Reconstructs the actual round-R pair set from the players'
    /// <c>ops</c> arrays, runs <see cref="UscfPairer"/> against a TRF
    /// document representing the section's state at the end of round
    /// R-1, and compares pair sets unordered (white/black agnostic).
    /// </summary>
    private static (bool caseSeen, bool matched, int actualCount, int matchedCount) CompareRound(
        StringBuilder sb, IReadOnlyList<NachPlayer> players, int round, string tournamentName, string date)
    {
        // Reconstruct actual pairings: a player at index i with ops[r] = j
        // means a game between (i+1, j) in round r+1. We canonicalise
        // to (lo, hi) and dedupe (each game shows up twice — once per
        // player).
        var actualSet = new HashSet<(int lo, int hi)>();
        var actualByes = new HashSet<int>();
        foreach (var p in players)
        {
            if (round - 1 >= p.Ops.Count) continue;
            var opp = p.Ops[round - 1];
            if (opp == 0)
            {
                actualByes.Add(p.PairNumber);
            }
            else
            {
                actualSet.Add(Norm(p.PairNumber, opp));
            }
        }

        // Roster for round R: anyone who has a non-zero opp in this
        // round (paired) OR who got a recorded bye/unpaired result.
        // Build a set of pair numbers we want in the TRF.
        var rosterPairs = new HashSet<int>();
        foreach (var pair in actualSet)
        {
            rosterPairs.Add(pair.lo);
            rosterPairs.Add(pair.hi);
        }
        // Players with explicit half-byes in this round get added too;
        // truly-absent players (withdrawals) stay out of the roster
        // entirely so the engine doesn't try to pair them.
        foreach (var p in players)
        {
            if (round - 1 >= p.Results.Count) continue;
            var res = p.Results[round - 1];
            if (res == 'H' || res == 'B') rosterPairs.Add(p.PairNumber);
            // 'U' = "unpaired" in NACH lingo — could be withdrawal or
            // zero-point bye. We don't include those in the roster
            // since the engine wouldn't have seen them either.
        }

        if (rosterPairs.Count == 0) return (false, false, 0, 0);

        // Build pre-flagged half-bye dict from this round's recorded
        // byes (mirrors UscfMultiRoundHarnessTests behaviour).
        var requestedByes = new Dictionary<int, char>();
        foreach (var p in players)
        {
            if (round - 1 >= p.Results.Count) continue;
            if (!rosterPairs.Contains(p.PairNumber)) continue;
            var res = p.Results[round - 1];
            if (res == 'H') requestedByes[p.PairNumber] = 'H';
        }

        // Roster filtered to in-section players, sorted by pair number
        // (engine canonical order).
        var roster = players.Where(p => rosterPairs.Contains(p.PairNumber))
                            .OrderBy(p => p.PairNumber)
                            .ToList();

        UscfPairingResult produced;
        try
        {
            var trf = BuildTrfDoc(roster, endedRound: round - 1, tournamentName, date)
                with { RequestedByes = requestedByes.Count == 0 ? null : requestedByes };
            produced = UscfPairer.Pair(trf);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  R{round}: !! engine threw {ex.GetType().Name}: {ex.Message}");
            return (true, false, actualSet.Count, 0);
        }

        var producedSet = new HashSet<(int lo, int hi)>(
            produced.Pairings.Select(p => Norm(p.WhitePair, p.BlackPair)));
        var matchedPairs = actualSet.Intersect(producedSet).Count();
        var matched = actualSet.SetEquals(producedSet);

        sb.Append($"  R{round}: ");
        if (matched)
        {
            sb.AppendLine($"✓ pair-set MATCH  ({actualSet.Count} pairs, " +
                          $"{(actualByes.Count > 0 ? $"{actualByes.Count} unpaired/byes" : "no byes")})");
        }
        else
        {
            sb.AppendLine($"✗ MISMATCH  ({matchedPairs}/{actualSet.Count} individual pairs match)");

            var actualOnly = actualSet.Except(producedSet).OrderBy(p => p.lo).ThenBy(p => p.hi).ToList();
            var producedOnly = producedSet.Except(actualSet).OrderBy(p => p.lo).ThenBy(p => p.hi).ToList();
            sb.AppendLine($"      only in SwissSys: {string.Join(", ", actualOnly.Select(p => $"({p.lo},{p.hi})"))}");
            sb.AppendLine($"      only in FreePair: {string.Join(", ", producedOnly.Select(p => $"({p.lo},{p.hi})"))}");
        }

        return (true, matched, actualSet.Count, matchedPairs);
    }

    private static TrfDocument BuildTrfDoc(
        IReadOnlyList<NachPlayer> roster, int endedRound,
        string tournamentName, string date)
    {
        var trfPlayers = roster
            .Select(p => new TrfPlayer(
                PairNumber: p.PairNumber,
                Name: p.Name,
                Rating: p.Rating,
                Id: p.Id,
                Points: ScoreThroughRound(p, endedRound),
                Rounds: BuildRoundCells(p, endedRound),
                Team: ""))    // NACH tournaments in our corpus have minimal team data; ignored here.
            .ToList();

        return new TrfDocument(
            TournamentName: tournamentName,
            StartDate: date,
            EndDate: date,
            TotalRounds: Math.Max(roster.Max(p => p.Results.Count) + 1, 1),
            InitialColor: 'w',
            Players: trfPlayers);
    }

    private static decimal ScoreThroughRound(NachPlayer p, int endedRound)
    {
        decimal score = 0m;
        var rounds = Math.Min(endedRound, p.Results.Count);
        for (var i = 0; i < rounds; i++)
        {
            score += p.Results[i] switch
            {
                'W' => 1m,
                'D' => 0.5m,
                'H' => 0.5m,
                'B' => 1m,    // full-point bye
                'L' => 0m,
                'U' => 0m,    // unpaired / zero-point
                _   => 0m,
            };
        }
        return score;
    }

    private static IReadOnlyList<TrfRoundCell> BuildRoundCells(NachPlayer p, int endedRound)
    {
        if (endedRound <= 0) return Array.Empty<TrfRoundCell>();
        var cells = new TrfRoundCell[endedRound];
        for (var r = 0; r < endedRound; r++)
        {
            if (r >= p.Results.Count)
            {
                cells[r] = TrfRoundCell.Empty;
                continue;
            }
            var color = r < p.Colors.Count ? p.Colors[r] switch
            {
                'W' => 'w',
                'B' => 'b',
                _   => '-',
            } : '-';
            var result = p.Results[r] switch
            {
                'W' => '1',
                'L' => '0',
                'D' => '=',
                'B' => 'U',  // TRF: U = unplayed full-point bye
                'H' => 'H',
                'U' => 'Z',  // unpaired -> zero-point in TRF
                _   => '-',
            };
            var opp = r < p.Ops.Count ? p.Ops[r] : 0;
            cells[r] = new TrfRoundCell(opp, color, result);
        }
        return cells;
    }

    private static (int lo, int hi) Norm(int a, int b) => a < b ? (a, b) : (b, a);

    private static string Pct(int num, int den) =>
        den == 0 ? "n/a" : $"{(100.0 * num / den):0.0}%";

    /// <summary>
    /// Lightweight DTO for a player parsed from the NACH publishing
    /// JSON format. Pair number is the 1-indexed position in the
    /// section's <c>players</c> array (the format doesn't carry pair
    /// numbers explicitly; opponents are referenced positionally).
    /// </summary>
    private sealed record NachPlayer(
        int PairNumber,
        string Name,
        int Rating,
        string Id,
        IReadOnlyList<int> Ops,
        IReadOnlyList<char> Colors,
        IReadOnlyList<char> Results)
    {
        public static NachPlayer From(JsonElement e, int pairNumber)
        {
            var name = e.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
            var rating = e.TryGetProperty("rating", out var rt) && rt.ValueKind == JsonValueKind.Number
                ? rt.GetInt32() : 0;
            var id = e.TryGetProperty("id1", out var idp) ? idp.GetString() ?? "" : "";

            var ops = ReadIntArray(e, "ops");
            var colors = ReadCharArray(e, "colors");
            var results = ReadCharArray(e, "results");
            return new NachPlayer(pairNumber, name, rating, id, ops, colors, results);
        }

        private static IReadOnlyList<int> ReadIntArray(JsonElement e, string prop)
        {
            if (!e.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array)
                return Array.Empty<int>();
            var list = new List<int>();
            foreach (var item in arr.EnumerateArray())
            {
                list.Add(item.ValueKind == JsonValueKind.Number ? item.GetInt32() : 0);
            }
            return list;
        }

        private static IReadOnlyList<char> ReadCharArray(JsonElement e, string prop)
        {
            if (!e.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array)
                return Array.Empty<char>();
            var list = new List<char>();
            foreach (var item in arr.EnumerateArray())
            {
                var s = item.GetString();
                list.Add(string.IsNullOrEmpty(s) ? '-' : s[0]);
            }
            return list;
        }
    }
}
