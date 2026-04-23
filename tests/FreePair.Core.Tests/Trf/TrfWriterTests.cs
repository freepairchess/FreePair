using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Trf;

namespace FreePair.Core.Tests.Trf;

public class TrfWriterTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task Write_emits_expected_header_lines()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var trf = TrfWriter.Write(t, openI);
        var lines = trf.Split('\n');

        Assert.Contains(lines, l => l.StartsWith("012 ") && l.Contains("Chess A2Z April Open 2026"));
        Assert.Contains(lines, l => l.StartsWith("042 ") && l.Contains("2026/04/04"));
        Assert.Contains(lines, l => l.StartsWith("052 ") && l.Contains("2026/04/04"));
        Assert.Contains(lines, l => l.StartsWith("062 ") && l.Contains("16"));
        Assert.Contains(lines, l => l.StartsWith("092 Individual: Swiss-System"));
        Assert.Contains(lines, l => l.StartsWith("122 ") && l.Contains("G/45"));
        Assert.Contains(lines, l => l.StartsWith("XXR "));
        Assert.Contains(lines, l => l.StartsWith("XXC white1"));
    }

    [Fact]
    public async Task Write_emits_XXC_black1_when_InitialColor_is_Black()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var trf = TrfWriter.Write(t, openI, InitialColor.Black);

        Assert.Contains("XXC black1", trf);
        Assert.DoesNotContain("XXC white1", trf);
    }

    [Fact]
    public async Task FormatPlayerLine_places_round1_cell_at_FIDE_columns_91_through_100()
    {
        // FIDE TRF-16 Annex 2: round cells are 10 chars wide starting at
        // column 91 (1-based). The cell's opponent field occupies columns
        // 92-95, so the leading character of the cell (col 91) must be a
        // blank separator. bbpPairings rejects the line otherwise.
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");
        var pair1 = openI.Players.Single(p => p.PairNumber == 1);

        var line = TrfWriter.FormatPlayerLine(pair1, openI.RoundsPlayed);

        // Round 1 cell lives at 0-based indices 90-99 (1-based 91-100).
        var r1 = line[90..100];
        Assert.Equal(10, r1.Length);
        Assert.Equal(' ', r1[0]);                   // col 91: blank separator
        Assert.Matches(@"^\s{3,4}\d+$", r1[1..5].TrimStart().PadLeft(4));
        Assert.Equal(' ', r1[5]);                   // col 96: blank
        Assert.Contains(r1[6], "wb-");              // col 97: colour
        Assert.Equal(' ', r1[7]);                   // col 98: blank
        Assert.Contains(r1[8], "10=UHF-");          // col 99: result code
        Assert.Equal(' ', r1[9]);                   // col 100: trailer
    }

    [Fact]
    public async Task Write_emits_one_001_line_per_player()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var trf = TrfWriter.Write(t, openI);
        var playerLines = trf.Split('\n').Where(l => l.StartsWith("001 ")).ToArray();

        Assert.Equal(16, playerLines.Length);
    }

    [Fact]
    public async Task Write_output_is_pure_ASCII()
    {
        var t = await LoadAsync();
        var openII = t.Sections.Single(s => s.Name == "Open II");

        var trf = TrfWriter.Write(t, openII);

        Assert.All(trf, c => Assert.InRange((int)c, 0x09, 0x7E));
    }

    [Fact]
    public void FormatPlayerLine_has_correct_fixed_columns()
    {
        var history = new[]
        {
            new RoundResult(RoundResultKind.Win,   9, PlayerColor.Black, 1, 0, 0, 0m),
            new RoundResult(RoundResultKind.Win,   4, PlayerColor.White, 1, 0, 0, 0m),
            new RoundResult(RoundResultKind.Win,   5, PlayerColor.Black, 1, 0, 0, 0m),
        };

        var player = new Player(
            PairNumber: 1,
            Name: "Maokhampio, Lucas",
            UscfId: "31368597",
            Rating: 1951,
            SecondaryRating: null,
            MembershipExpiration: null,
            Club: null,
            State: null,
            Team: null,
            RequestedByeRounds: System.Array.Empty<int>(),
            History: history);

        var line = TrfWriter.FormatPlayerLine(player, roundsPlayed: 3);

        // "001" tag
        Assert.StartsWith("001 ", line);

        // Starting rank at columns 5-8 (right-justified). Column 8 is index 7.
        Assert.Equal('1', line[7]);

        // Name begins at column 15 (index 14).
        Assert.Equal("Maokhampio, Lucas", line.Substring(14, "Maokhampio, Lucas".Length));

        // Rating "1951" ends at column 52.
        Assert.Equal("1951", line.Substring(48, 4));

        // First round cell should contain opponent 9, colour b, result 1.
        Assert.Contains("   9 b 1", line);
        // Second round cell.
        Assert.Contains("   4 w 1", line);
        // Third round cell.
        Assert.Contains("   5 b 1", line);

        // Points at columns 81-84: " 3.0" (trimmed output still has the field).
        Assert.Contains(" 3.0", line);
    }

    [Fact]
    public void FormatPlayerLine_renders_draw_and_bye_codes()
    {
        var history = new[]
        {
            new RoundResult(RoundResultKind.HalfPointBye, -1, PlayerColor.None, 0, 0, 0, 0m),
            new RoundResult(RoundResultKind.Draw,         16, PlayerColor.White, 3, 0, 0, 0m),
            new RoundResult(RoundResultKind.FullPointBye, -1, PlayerColor.None, 0, 0, 0, 0m),
        };

        var player = new Player(
            PairNumber: 5,
            Name: "Test, Player",
            UscfId: null,
            Rating: 1200,
            SecondaryRating: null,
            MembershipExpiration: null,
            Club: null,
            State: null,
            Team: null,
            RequestedByeRounds: System.Array.Empty<int>(),
            History: history);

        var line = TrfWriter.FormatPlayerLine(player, roundsPlayed: 3);

        Assert.Contains("0000 - H", line); // half-point bye
        Assert.Contains("  16 w =", line); // draw as white vs pair 16
        Assert.Contains("0000 - U", line); // full-point bye (FIDE "U")
    }

    [Fact]
    public void FormatPlayerLine_sanitizes_non_ASCII_names()
    {
        var player = new Player(
            PairNumber: 1,
            Name: "Kašparov, Garry",
            UscfId: "12345",
            Rating: 2800,
            SecondaryRating: null,
            MembershipExpiration: null,
            Club: null,
            State: null,
            Team: null,
            RequestedByeRounds: System.Array.Empty<int>(),
            History: System.Array.Empty<RoundResult>());

        var line = TrfWriter.FormatPlayerLine(player, roundsPlayed: 0);

        Assert.All(line, c => Assert.InRange((int)c, 0x09, 0x7E));
        Assert.Contains("Ka?parov", line);
    }

    // ---------------------------------------------------------------
    // Requested-bye enforcement (SwissSys parity feature #1).
    //
    // When a player has pre-requested a half-point bye for the round
    // about to be paired, the TRF line must carry an extra round cell
    // at position <pairingRound> with opponent=0000, color='-',
    // result='H'. bbpPairings treats that as "player is already
    // scheduled for H in round N; don't try to pair them".
    // ---------------------------------------------------------------

    [Fact]
    public void FormatPlayerLine_emits_H_cell_for_requested_bye_at_pairing_round()
    {
        // Player with 2 rounds of history and a requested ½-pt bye for
        // round 3. When we ask TRF to pair round 3, cell 3 must be H.
        var history = new[]
        {
            new RoundResult(RoundResultKind.Win,  12, PlayerColor.White, 1, 0, 0, 0m),
            new RoundResult(RoundResultKind.Draw, 8,  PlayerColor.Black, 1, 0, 0, 0m),
        };

        var player = new Player(
            PairNumber: 1,
            Name: "Alice",
            UscfId: null,
            Rating: 1800,
            SecondaryRating: null,
            MembershipExpiration: null,
            Club: null,
            State: null,
            Team: null,
            RequestedByeRounds: new[] { 3 },
            History: history);

        var line = TrfWriter.FormatPlayerLine(player, roundsPlayed: 2, pairingRound: 3);

        // Must still have both history cells AND a new H cell.
        Assert.Contains("  12 w 1", line);  // round 1 history
        Assert.Contains("   8 b =", line);  // round 2 history
        Assert.Contains("0000 - H", line);  // round 3 pre-bye
    }

    [Fact]
    public void FormatPlayerLine_skips_H_cell_when_player_has_no_requested_bye_for_pairing_round()
    {
        var player = new Player(
            PairNumber: 1,
            Name: "Alice",
            UscfId: null,
            Rating: 1800,
            SecondaryRating: null,
            MembershipExpiration: null,
            Club: null,
            State: null,
            Team: null,
            RequestedByeRounds: new[] { 5 }, // asks for bye in round 5, not 3
            History: System.Array.Empty<RoundResult>());

        var line = TrfWriter.FormatPlayerLine(player, roundsPlayed: 0, pairingRound: 3);

        // No H cell because requested bye is for round 5, not the one
        // being paired.
        Assert.DoesNotContain("0000 - H", line);
    }

    [Fact]
    public async Task Write_emits_H_cells_for_players_with_requested_byes_at_next_round()
    {
        // The sample has 3 rounds already played. The "next round to
        // pair" is therefore round 4. Seed one player with a requested
        // bye for round 4 and confirm the emitted TRF carries the H.
        var t = await LoadAsync();
        var section = t.Sections.Single(s => s.Name == "Open I");

        const int pairingRound = 4;
        var first = section.Players.First();
        var patched = first with { RequestedByeRounds = new[] { pairingRound } };
        var patchedSection = section with
        {
            Players = section.Players
                .Select(p => p.PairNumber == first.PairNumber ? patched : p)
                .ToArray(),
        };
        var patchedTournament = t with
        {
            Sections = t.Sections
                .Select(s => s.Name == section.Name ? patchedSection : s)
                .ToArray(),
        };

        var trf = TrfWriter.Write(
            patchedTournament,
            patchedSection,
            pairingRound: pairingRound);

        // Find the first player's 001 line by pair number (unambiguous).
        var firstPairSlug = $"001 {first.PairNumber,4}";
        var firstLine = trf.Split('\n').Single(l => l.StartsWith(firstPairSlug));
        Assert.Contains("0000 - H", firstLine);

        // A non-bye player's line must NOT have an H cell.
        var other = section.Players.Skip(1).First();
        var otherPairSlug = $"001 {other.PairNumber,4}";
        var otherLine = trf.Split('\n').Single(l => l.StartsWith(otherPairSlug));
        Assert.DoesNotContain("0000 - H", otherLine);
    }

    // ---------------------------------------------------------------
    // Withdrawals (SwissSys parity feature #7).
    //
    // Withdrawn players must disappear from the TRF entirely so BBP
    // never tries to pair them, and the 062 player-count line has to
    // reflect only the surviving pool.
    // ---------------------------------------------------------------

    [Fact]
    public async Task Write_excludes_withdrawn_players_from_001_lines_and_062_count()
    {
        var t = await LoadAsync();
        var section = t.Sections.Single(s => s.Name == "Open I");

        var victim = section.Players.First();
        var withdrawn = t.Sections
            .Single(s => s.Name == "Open I")
            .Players
            .Select(p => p.PairNumber == victim.PairNumber
                ? p with { Withdrawn = true }
                : p)
            .ToArray();
        var patchedSection = section with { Players = withdrawn };
        var patchedTournament = t with
        {
            Sections = t.Sections
                .Select(s => s.Name == section.Name ? patchedSection : s)
                .ToArray(),
        };

        var trf = TrfWriter.Write(patchedTournament, patchedSection);
        var lines = trf.Split('\n');

        // 062 must report the active count (one less than total).
        var header = lines.Single(l => l.StartsWith("062 "));
        Assert.Contains((section.Players.Count - 1).ToString(), header);

        // Withdrawn player's 001 line is absent.
        var victimSlug = $"001 {victim.PairNumber,4}";
        Assert.DoesNotContain(lines, l => l.StartsWith(victimSlug));

        // Every other player's 001 line is still present.
        var pair001Lines = lines.Count(l => l.StartsWith("001 "));
        Assert.Equal(section.Players.Count - 1, pair001Lines);
    }
}
