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
/// Diagnostic side-by-side dump for the MCC (Massachusetts Chess Club)
/// subset of the USCF corpus. Adult open events with no scholastic
/// sibling tagging — almost every Player.Team is blank — so any
/// pair-set mismatch with SwissSys is purely algorithmic, not a
/// constraint difference. Used after the Puddletown investigation
/// to isolate the specific score-group / float / colour rules where
/// our engine and SwissSys disagree.
/// </summary>
/// <remarks>
/// <para>For every round 2+ in every MCC sample, prints (to test
/// output):</para>
/// <list type="bullet">
///   <item>Each player's score at the start of the round (sorted
///         high-to-low, ties broken by rating then pair).</item>
///   <item>SwissSys's actual pairings.</item>
///   <item>FreePair-USCF's pairings.</item>
///   <item>The unordered-pair-set DIFF: which matchups are unique to
///         each side. When both sets match, the round is reported as
///         a pair-set match (boards/colours may still differ).</item>
/// </list>
/// <para>Purely informational — never fails. Triage tool for
/// reverse-engineering SwissSys's specific algorithm choices.</para>
/// </remarks>
public class UscfMccDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public UscfMccDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async System.Threading.Tasks.Task Mcc_subset_side_by_side_pairing_dump()
    {
        var files = UscfSampleDiscovery.FinalStateFiles()
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return name.StartsWith("MCC", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            _output.WriteLine("(no MCC samples found — diagnostic is a no-op)");
            return;
        }

        var loader = new TournamentLoader();
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("===== MCC side-by-side pairing diagnostic =====");

        var totalCases = 0;
        var matchCases = 0;

        foreach (var file in files)
        {
            Tournament tournament;
            try
            {
                tournament = await loader.LoadAsync(file).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"!! load failed {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            sb.AppendLine();
            sb.AppendLine($"### {Path.GetFileName(file)}");

            foreach (var section in tournament.Sections)
            {
                if (section.SoftDeleted) continue;
                if (section.Players.Count < 2) continue;
                if (section.RoundsPlayed < 2) continue;

                for (var round = 2; round <= section.RoundsPlayed; round++)
                {
                    var (caseSeen, matched) = DumpRound(sb, tournament, section, round);
                    if (caseSeen) totalCases++;
                    if (matched) matchCases++;
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine($"---- summary ----");
        sb.AppendLine($"comparable rounds  : {totalCases}");
        sb.AppendLine($"pair-set matches   : {matchCases}  ({(totalCases == 0 ? "n/a" : $"{(100.0 * matchCases / totalCases):0.0}%")})");
        sb.AppendLine($"hard mismatches    : {totalCases - matchCases}");

        _output.WriteLine(sb.ToString());
    }

    private static (bool caseSeen, bool matched) DumpRound(
        StringBuilder sb, Tournament tournament, Section section, int round)
    {
        var roundEntry = section.Rounds.FirstOrDefault(r => r.Number == round);
        if (roundEntry is null) return (false, false);

        // Roster = anyone paired or bye'd in this round.
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
        if (roster.Length == 0) return (false, false);

        // Build TRF, run engine.
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
        catch (Exception ex)
        {
            sb.AppendLine();
            sb.AppendLine($"  [{section.Name}] R{round}: !! engine threw {ex.GetType().Name}: {ex.Message}");
            return (false, false);
        }

        // Pair sets (unordered, white/black agnostic).
        var actualPairs = roundEntry.Pairings
            .Select(p => Norm(p.WhitePair, p.BlackPair))
            .ToList();
        var producedPairs = produced.Pairings
            .Select(p => Norm(p.WhitePair, p.BlackPair))
            .ToList();
        var actualSet = new HashSet<(int, int)>(actualPairs);
        var producedSet = new HashSet<(int, int)>(producedPairs);
        var matched = actualSet.SetEquals(producedSet);

        // Only print mismatches in detail; matches get a one-liner.
        sb.AppendLine();
        if (matched)
        {
            sb.AppendLine($"  [{section.Name}] R{round}: ✓ pair-set match  ({roster.Length} players)");
            return (true, true);
        }

        sb.AppendLine($"  [{section.Name}] R{round}: ✗ pair-set MISMATCH  ({roster.Length} players)");

        // Score table.
        var scored = roster
            .Select(p => new
            {
                Pair = p.PairNumber,
                Name = AbbrevName(p.Name),
                Rating = p.Rating,
                Score = ScoreThroughRound(p, round - 1),
                Team = p.Team ?? "",
            })
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.Rating)
            .ThenBy(p => p.Pair)
            .ToList();

        sb.AppendLine($"    Pre-R{round} standings (score, rating, pair, name):");
        foreach (var p in scored)
        {
            var teamSuffix = string.IsNullOrEmpty(p.Team) ? "" : $"  team='{p.Team}'";
            sb.AppendLine($"      {p.Score,4:0.0}  {p.Rating,4}  #{p.Pair,-3}  {p.Name}{teamSuffix}");
        }

        // Pairings side-by-side, ordered by SwissSys's board.
        sb.AppendLine($"    SwissSys actual:");
        foreach (var p in roundEntry.Pairings.OrderBy(p => p.Board))
        {
            var w = ScoreShort(scored, p.WhitePair);
            var b = ScoreShort(scored, p.BlackPair);
            sb.AppendLine($"      bd {p.Board,3}: #{p.WhitePair,-3}({w}) W  vs  #{p.BlackPair,-3}({b}) B");
        }
        foreach (var b in roundEntry.Byes)
        {
            var s = ScoreShort(scored, b.PlayerPair);
            sb.AppendLine($"      bye:    #{b.PlayerPair,-3}({s})  [{b.Kind}]");
        }

        sb.AppendLine($"    FreePair USCF produced:");
        foreach (var p in produced.Pairings.OrderBy(p => p.Board))
        {
            var w = ScoreShort(scored, p.WhitePair);
            var b = ScoreShort(scored, p.BlackPair);
            sb.AppendLine($"      bd {p.Board,3}: #{p.WhitePair,-3}({w}) W  vs  #{p.BlackPair,-3}({b}) B");
        }
        if (produced.ByePair is int bp)
        {
            var s = ScoreShort(scored, bp);
            sb.AppendLine($"      bye:    #{bp,-3}({s})  [Full]");
        }
        foreach (var rb in produced.RequestedByesOrEmpty)
        {
            var s = ScoreShort(scored, rb.PairNumber);
            sb.AppendLine($"      bye:    #{rb.PairNumber,-3}({s})  [{rb.Kind}]");
        }

        // Pair-set diff.
        var actualOnly = actualSet.Except(producedSet).ToList();
        var producedOnly = producedSet.Except(actualSet).ToList();
        sb.AppendLine($"    DIFF — only in SwissSys: {string.Join(", ", actualOnly.Select(p => $"({p.Item1},{p.Item2})"))}");
        sb.AppendLine($"    DIFF — only in FreePair: {string.Join(", ", producedOnly.Select(p => $"({p.Item1},{p.Item2})"))}");

        return (true, false);
    }

    private static string ScoreShort(IEnumerable<dynamic> scored, int pair)
    {
        foreach (var p in scored) if ((int)p.Pair == pair) return ((decimal)p.Score).ToString("0.#", CultureInfo.InvariantCulture);
        return "?";
    }

    private static string AbbrevName(string name)
    {
        // "Last, First Middle" → "Last, F."
        var comma = name.IndexOf(',');
        if (comma <= 0 || comma >= name.Length - 1) return name;
        var last = name.Substring(0, comma).Trim();
        var firstChunk = name.Substring(comma + 1).Trim();
        var initial = firstChunk.Length > 0 ? firstChunk[0].ToString() + "." : "";
        return $"{last}, {initial}";
    }

    private static (int lo, int hi) Norm(int a, int b) => a < b ? (a, b) : (b, a);

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
}
