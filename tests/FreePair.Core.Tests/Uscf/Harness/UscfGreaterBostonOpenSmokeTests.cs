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
/// End-to-end demonstration: run the FreePair USCF engine on the
/// "90th Greater Boston Open" pre-pairing snapshot and dump every
/// section's round-1 output. Purely informational (never fails) —
/// there is no SwissSys oracle to compare against because the file
/// captures the tournament BEFORE round 1 was paired in SwissSys
/// (Rounds played = Rounds paired = 0; players carry no Results,
/// no byes, no team flags beyond the lone scholastic-club tags
/// the TD entered up-front).
/// </summary>
/// <remarks>
/// <para>The dump shows, per section:</para>
/// <list type="bullet">
///   <item>Section header (player count, total rounds).</item>
///   <item>Top half / bottom half split as the engine sees it after
///         the rating-desc + pair-asc sort.</item>
///   <item>Round-1 pairings in board order with each player's rating.</item>
///   <item>Same-team summary — flags any board where both players
///         share a team label so the TD can sanity-check the
///         constraint pipeline.</item>
/// </list>
/// </remarks>
public class UscfGreaterBostonOpenSmokeTests
{
    private readonly ITestOutputHelper _output;

    public UscfGreaterBostonOpenSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async System.Threading.Tasks.Task Greater_Boston_Open_round_1_smoke_dump()
    {
        var file = UscfSampleDiscovery.FinalStateFiles()
            .FirstOrDefault(f => Path.GetFileName(f).Equals(
                "90th_Greater_Boston_Open.sjson", StringComparison.OrdinalIgnoreCase));

        if (file is null)
        {
            _output.WriteLine("(90th Greater Boston Open sample not found — smoke test is a no-op)");
            return;
        }

        var loader = new TournamentLoader();
        var tournament = await loader.LoadAsync(file).ConfigureAwait(true);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"===== {Path.GetFileName(file)} :: round-1 engine output =====");
        sb.AppendLine($"Title: {tournament.Title}");
        sb.AppendLine($"Sections: {tournament.Sections.Count(s => !s.SoftDeleted)}");
        sb.AppendLine();

        var totalPairs = 0;
        var totalSameTeamPairs = 0;

        foreach (var section in tournament.Sections.Where(s => !s.SoftDeleted))
        {
            DumpSection(sb, tournament, section, ref totalPairs, ref totalSameTeamPairs);
        }

        sb.AppendLine($"---- summary ----");
        sb.AppendLine($"total boards paired across all sections: {totalPairs}");
        sb.AppendLine($"boards where both players share a team:  {totalSameTeamPairs}");
        if (totalSameTeamPairs == 0)
        {
            sb.AppendLine("(all team-tagged players ended up paired with non-team players — sibling avoidance worked)");
        }

        _output.WriteLine(sb.ToString());
    }

    private static void DumpSection(
        StringBuilder sb, Tournament tournament, Section section,
        ref int totalPairs, ref int totalSameTeamPairs)
    {
        var roster = section.Players.Where(p => !p.SoftDeleted).ToArray();
        if (roster.Length < 2)
        {
            sb.AppendLine($"### [{section.Name}]  ({roster.Length} players — too few to pair)");
            sb.AppendLine();
            return;
        }

        // Build a TRF doc representing the pre-tournament state — no
        // played rounds, so the engine takes the round-1 path.
        var trfPlayers = roster
            .OrderBy(p => p.PairNumber)
            .Select(p => new TrfPlayer(
                PairNumber: p.PairNumber,
                Name: p.Name,
                Rating: p.Rating,
                Id: p.UscfId ?? string.Empty,
                Points: 0m,
                Rounds: Array.Empty<TrfRoundCell>(),
                Team: p.Team ?? string.Empty))
            .ToList();

        var trf = new TrfDocument(
            TournamentName: tournament.Title ?? section.Name,
            StartDate: string.Empty,
            EndDate: string.Empty,
            TotalRounds: Math.Max(section.FinalRound, 1),
            InitialColor: 'w',
            Players: trfPlayers);

        UscfPairingResult result;
        try
        {
            result = UscfPairer.Pair(trf);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"### [{section.Name}]  ({roster.Length} players)");
            sb.AppendLine($"  !! engine threw {ex.GetType().Name}: {ex.Message}");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"### [{section.Name}]  ({roster.Length} players, {section.FinalRound} rounds planned)");

        // Show the rating-sorted order with a half-divider so the TD
        // can see how the engine grouped top half vs bottom half.
        var sorted = trfPlayers.OrderByDescending(p => p.Rating).ThenBy(p => p.PairNumber).ToList();
        var half = sorted.Count / 2;
        sb.AppendLine($"  Rating-sorted seeding (top half over bottom half):");
        for (var i = 0; i < sorted.Count; i++)
        {
            var p = sorted[i];
            var team = string.IsNullOrEmpty(p.Team) ? "" : $"  team='{p.Team}'";
            var marker = i == half ? "  --- half ---" : "";
            sb.AppendLine($"    seed {i + 1,2}:  {p.Rating,4}  #{p.PairNumber,-3}  {Abbrev(p.Name)}{team}{marker}");
        }

        sb.AppendLine($"  Round-1 pairings:");
        var teamLookup = trfPlayers.ToDictionary(p => p.PairNumber, p => p.Team);
        var sectionSameTeam = 0;
        foreach (var pairing in result.Pairings.OrderBy(p => p.Board))
        {
            var wTeam = teamLookup.TryGetValue(pairing.WhitePair, out var wt) ? wt : "";
            var bTeam = teamLookup.TryGetValue(pairing.BlackPair, out var bt) ? bt : "";
            var sameTeamFlag = !string.IsNullOrWhiteSpace(wTeam)
                            && string.Equals(wTeam, bTeam, StringComparison.OrdinalIgnoreCase)
                            ? "  ⚠ SAME TEAM"
                            : "";
            var wRating = trfPlayers.First(p => p.PairNumber == pairing.WhitePair).Rating;
            var bRating = trfPlayers.First(p => p.PairNumber == pairing.BlackPair).Rating;
            sb.AppendLine($"    bd {pairing.Board,2}:  #{pairing.WhitePair,-3}({wRating}) W  vs  #{pairing.BlackPair,-3}({bRating}) B{sameTeamFlag}");
            if (sameTeamFlag.Length > 0) sectionSameTeam++;
        }
        if (result.ByePair is int bp)
        {
            var bRating = trfPlayers.First(p => p.PairNumber == bp).Rating;
            sb.AppendLine($"    bye:    #{bp,-3}({bRating})  [Full-point — odd field]");
        }

        sb.AppendLine();
        totalPairs += result.Pairings.Count;
        totalSameTeamPairs += sectionSameTeam;
    }

    private static string Abbrev(string name)
    {
        var comma = name.IndexOf(',');
        if (comma <= 0 || comma >= name.Length - 1) return name;
        var last = name.Substring(0, comma).Trim();
        var firstChunk = name.Substring(comma + 1).Trim();
        var initial = firstChunk.Length > 0 ? firstChunk[0].ToString() + "." : "";
        return $"{last}, {initial}";
    }
}
