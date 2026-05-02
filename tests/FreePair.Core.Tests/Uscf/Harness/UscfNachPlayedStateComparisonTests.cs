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
    /// </summary>
    private const int MinExpectedMatchedRounds = 61;
    private const int MinExpectedMatchedPairs  = 2269;

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
