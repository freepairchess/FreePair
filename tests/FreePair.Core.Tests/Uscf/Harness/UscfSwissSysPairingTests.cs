using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;
using Xunit.Abstractions;

namespace FreePair.Core.Tests.Uscf.Harness;

/// <summary>
/// Parameterized pairing regression tests driven by curated SwissSys SJSON
/// files. Each test case represents one (file × section × round) triple and
/// asserts that <see cref="UscfPairer.Pair"/> reproduces the exact pairings
/// that SwissSys produced — same pair-set, same bye, same board order, and
/// same colour assignment.
/// </summary>
/// <remarks>
/// <para>Test fixtures live in
/// <c>tests/FreePair.Core.Tests/Data/SwissSysTestCases/USCFEvents/</c>.
/// Adding a new SJSON file there automatically generates new test cases.</para>
/// <para>The goal is 100% match with these curated files. Any regression
/// means a test failure — no tolerance thresholds.</para>
/// </remarks>
public class UscfSwissSysPairingTests
{
    private readonly ITestOutputHelper _output;

    public UscfSwissSysPairingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> TestCases()
    {
        var root = Path.Combine(TestPaths.RepoRoot, "tests", "FreePair.Core.Tests", "Data", "SwissSysTestCases", "USCFEvents");
        if (!Directory.Exists(root))
            yield break;

        var loader = new TournamentLoader();

        foreach (var file in Directory.EnumerateFiles(root, "*.sjson", SearchOption.AllDirectories))
        {
            Tournament tournament;
            try
            {
                tournament = loader.LoadAsync(file).GetAwaiter().GetResult();
            }
            catch
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(root, file);

            foreach (var section in tournament.Sections)
            {
                if (section.SoftDeleted) continue;
                if (section.RoundsPlayed < 1) continue;

                for (var round = 1; round <= section.RoundsPlayed; round++)
                {
                    yield return [relativePath, section.Name, round];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public async System.Threading.Tasks.Task Pairing_matches_SwissSys(string relativePath, string sectionName, int round)
    {
        var root = Path.Combine(TestPaths.RepoRoot, "tests", "FreePair.Core.Tests", "Data", "SwissSysTestCases", "USCFEvents");
        var file = Path.Combine(root, relativePath);

        var loader = new TournamentLoader();
        var tournament = await loader.LoadAsync(file);

        var section = tournament.Sections.First(s => s.Name == sectionName);
        var roundEntry = section.Rounds.First(r => r.Number == round);

        // Build the actual pairings from the SJSON (what SwissSys produced)
        var actual = ExtractActuals(roundEntry, section.FirstBoard ?? 1);

        // Build the round pool — players who participated in this round
        var roundPool = new HashSet<int>();
        foreach (var p in actual.Pairings)
        {
            roundPool.Add(p.WhitePair);
            roundPool.Add(p.BlackPair);
        }
        if (actual.ByePair is int byePair) roundPool.Add(byePair);
        foreach (var rb in actual.RequestedByes) roundPool.Add(rb.PairNumber);

        var roster = section.Players
            .Where(p => !p.SoftDeleted && roundPool.Contains(p.PairNumber))
            .ToArray();

        // Build TRF with history through round-1 (for round 1: empty history)
        // When AvoidSameTeam is off, clear team strings so the pairer
        // doesn't try to avoid same-team pairings.
        var trf = BuildTrfDocAtEndOfRound(tournament, section, roster,
            endedRound: round - 1, includeTeams: section.AvoidSameTeam);

        // Attach requested byes
        var requestedByesDict = new Dictionary<int, char>();
        foreach (var rb in actual.RequestedByes)
        {
            requestedByesDict[rb.PairNumber] = rb.Kind;
        }
        if (requestedByesDict.Count > 0)
        {
            trf = trf with { RequestedByes = requestedByesDict };
        }

        // Use the section's initial colour setting so the engine assigns
        // colours consistently with what SwissSys recorded.
        if (section.InitialColor is InitialColor ic)
        {
            trf = trf with { InitialColor = ic == InitialColor.Black ? 'b' : 'w' };
        }

        // Pair
        var produced = UscfPairer.Pair(trf);

        // Compare — both overall and per-pairing
        var comparison = Compare(actual, produced);
        var pairingStats = CountPairingMatches(actual, produced);

        _output.WriteLine($"{relativePath} :: {sectionName} :: R{round}  " +
            $"[{pairingStats.matched}/{pairingStats.total} pairings match" +
            (section.AvoidSameTeam ? ", avoid-same-team ON" : "") +
            $"]");

        if (!comparison.IsExactMatch)
        {
            _output.WriteLine($"  pair-set match: {comparison.PairsMatchUnordered}");
            _output.WriteLine($"  bye match: {comparison.ByesMatch}");
            _output.WriteLine($"  board+colour: {comparison.BoardsAndColorsMatch}");
            _output.WriteLine("  SwissSys actual:");
            foreach (var p in actual.Pairings.OrderBy(p => p.Board))
            {
                _output.WriteLine($"    bd {p.Board}: {p.WhitePair,3} W vs {p.BlackPair,3} B");
            }
            if (actual.ByePair is int ab) _output.WriteLine($"    bye: {ab}");
            _output.WriteLine("  FreePair produced:");
            foreach (var p in produced.Pairings.OrderBy(p => p.Board))
            {
                _output.WriteLine($"    bd {p.Board}: {p.WhitePair,3} W vs {p.BlackPair,3} B");
            }
            if (produced.ByePair is int pb) _output.WriteLine($"    bye: {pb}");
        }

        Assert.True(comparison.IsExactMatch,
            $"Pairing mismatch for {relativePath} :: {sectionName} :: R{round}. " +
            $"PairSet={comparison.PairsMatchUnordered}, Bye={comparison.ByesMatch}, BoardColor={comparison.BoardsAndColorsMatch}. " +
            $"Pairings matched: {pairingStats.matched}/{pairingStats.total}");
    }

    // ----------------------------------------------------------------- helpers

    private sealed record RoundActuals(
        IReadOnlyList<UscfPairing> Pairings,
        int? ByePair,
        IReadOnlyList<UscfRequestedBye> RequestedByes);

    private static RoundActuals ExtractActuals(Round round, int sectionFirstBoard)
    {
        var offset = sectionFirstBoard - 1;
        var pairings = round.Pairings
            .Select(p => new UscfPairing(p.WhitePair, p.BlackPair, p.Board - offset))
            .ToArray();
        var bye = round.Byes.FirstOrDefault(b => b.Kind == ByeKind.Full);

        var requestedByes = round.Byes
            .Where(b => b.Kind == ByeKind.Half || b.Kind == ByeKind.Unpaired)
            .Select(b => new UscfRequestedBye(
                b.PlayerPair,
                b.Kind == ByeKind.Half ? 'H' : 'Z'))
            .OrderBy(b => b.PairNumber)
            .ToArray();

        return new RoundActuals(pairings, bye?.PlayerPair, requestedByes);
    }

    private static TrfDocument BuildTrfDocAtEndOfRound(
        Tournament tournament,
        Section section,
        IReadOnlyList<Player> roster,
        int endedRound,
        bool includeTeams = true)
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
                Team: includeTeams ? (p.Team ?? string.Empty) : string.Empty,
                HasScheduledBye:
                    (p.RequestedByeRounds?.Count > 0) ||
                    (p.ZeroPointByeRoundsOrEmpty.Count > 0)))
            .ToList();

        return new TrfDocument(
            TournamentName: tournament.Title ?? section.Name,
            StartDate: tournament.StartDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            EndDate: tournament.EndDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? string.Empty,
            TotalRounds: Math.Max(section.FinalRound, section.RoundsPlayed),
            InitialColor: 'w',
            Players: trfPlayers);
    }

    /// <summary>
    /// Counts how many individual pairings (unordered pair-sets) match
    /// between the actual and produced results.
    /// </summary>
    private static (int matched, int total) CountPairingMatches(
        RoundActuals actual, UscfPairingResult produced)
    {
        var actualSet = new HashSet<(int, int)>(actual.Pairings
            .Select(p => Normalize(p.WhitePair, p.BlackPair)));
        var producedSet = new HashSet<(int, int)>(produced.Pairings
            .Select(p => Normalize(p.WhitePair, p.BlackPair)));

        var total = actualSet.Count;
        var matched = actualSet.Count(p => producedSet.Contains(p));
        return (matched, total);

        static (int, int) Normalize(int a, int b) =>
            a < b ? (a, b) : (b, a);
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
            RoundResultKind.FullPointBye => 'U',
            RoundResultKind.HalfPointBye => 'H',
            RoundResultKind.ZeroPointBye => 'Z',
            _ => '-',
        };

        return new TrfRoundCell(rr.Opponent, color, result);
    }

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

        var fullByesMatch = actual.ByePair == produced.ByePair;
        var actualReqSet = new HashSet<(int, char)>(actual.RequestedByes.Select(b => (b.PairNumber, b.Kind)));
        var producedReqSet = new HashSet<(int, char)>(produced.RequestedByesOrEmpty.Select(b => (b.PairNumber, b.Kind)));
        var requestedByesMatch = actualReqSet.SetEquals(producedReqSet);
        var byesMatch = fullByesMatch && requestedByesMatch;

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
