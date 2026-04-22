using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Trf;

/// <summary>
/// Writes a <see cref="Tournaments.Tournament"/> section to a FIDE-style
/// Tournament Report File (TRF, FIDE handbook C.04 Annex 2). The output is
/// pure ASCII and follows the fixed-column layout that pairing engines such
/// as BBP (<c>bbpPairings</c>) and JaVaFo expect.
/// </summary>
/// <remarks>
/// <para>Scope of this writer (v1):</para>
/// <list type="bullet">
///   <item>Tournament header lines 012 / 022 / 042 / 052 / 062 / 092 / 122.</item>
///   <item>Number-of-rounds line (XXR) used by pairing engines.</item>
///   <item>One 001 player line per section player with per-round opponent /
///         colour / result cells.</item>
/// </list>
/// <para>Scores and results are always emitted in ASCII regardless of the
/// app-wide display preference — FIDE TRF mandates ASCII.</para>
/// </remarks>
public static class TrfWriter
{
    /// <summary>
    /// Writes one <paramref name="section"/> of <paramref name="tournament"/>
    /// as a TRF document to <paramref name="writer"/>.
    /// </summary>
    /// <param name="initialColor">
    /// The colour the top seed is to receive on board 1 of round 1 (emitted
    /// as the <c>XXC</c> TRF directive). Required by pairing engines to seed
    /// colour allocation; ignored by engines once history exists.
    /// </param>
    public static void Write(
        Tournaments.Tournament tournament,
        Tournaments.Section section,
        TextWriter writer,
        InitialColor initialColor = InitialColor.White)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(writer);

        WriteHeader(tournament, section, writer);
        WriteNumberOfRounds(section, writer, initialColor);
        WritePlayers(section, writer);
    }

    /// <summary>
    /// Convenience overload that renders to an in-memory string.
    /// </summary>
    public static string Write(
        Tournaments.Tournament tournament,
        Tournaments.Section section,
        InitialColor initialColor = InitialColor.White)
    {
        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        sw.NewLine = "\n";
        Write(tournament, section, sw, initialColor);
        return sw.ToString();
    }

    private static void WriteHeader(
        Tournaments.Tournament t,
        Tournaments.Section s,
        TextWriter w)
    {
        // 012 Tournament name (combined with section title for disambiguation).
        var title = string.IsNullOrWhiteSpace(t.Title)
            ? s.Name
            : $"{t.Title} - {s.Name}";
        WriteLine(w, "012", title);

        // 022 City — not modelled yet.
        WriteLine(w, "022", string.Empty);

        // 042 Start date / 052 End date (ISO yyyy/MM/dd expected by TRF).
        WriteLine(w, "042", FormatDate(t.StartDate));
        WriteLine(w, "052", FormatDate(t.EndDate));

        // 062 Number of players.
        WriteLine(w, "062", s.Players.Count.ToString(CultureInfo.InvariantCulture));

        // 092 Type of tournament.
        WriteLine(w, "092", "Individual: Swiss-System");

        // 122 Time control.
        if (!string.IsNullOrWhiteSpace(s.TimeControl))
        {
            WriteLine(w, "122", s.TimeControl!);
        }
    }

    private static void WriteNumberOfRounds(Tournaments.Section s, TextWriter w, InitialColor initialColor)
    {
        // "XXR" is the common extension recognised by pairing engines for
        // "number of rounds in the tournament". We emit the greater of paired
        // and played so engines know how many rounds total to pair toward.
        var totalRounds = Math.Max(s.RoundsPaired, s.FinalRound);
        if (totalRounds <= 0)
        {
            totalRounds = s.RoundsPlayed;
        }

        WriteLine(w, "XXR", totalRounds.ToString(CultureInfo.InvariantCulture));

        // "XXC" tells the engine the initial piece colour for the top player
        // on board 1 of round 1. Required by bbpPairings for round-1 pairing.
        var colorDirective = initialColor == InitialColor.Black ? "black1" : "white1";
        WriteLine(w, "XXC", colorDirective);
    }

    private static void WritePlayers(Tournaments.Section s, TextWriter w)
    {
        foreach (var p in s.Players.OrderBy(x => x.PairNumber))
        {
            w.WriteLine(FormatPlayerLine(p, s.RoundsPlayed));
        }
    }

    /// <summary>
    /// Formats a single TRF <c>001</c> player line per FIDE TRF-16 column
    /// positions. Exposed as <c>internal</c> for direct unit testing.
    /// </summary>
    internal static string FormatPlayerLine(Tournaments.Player player, int roundsPlayed)
    {
        // TRF-16 fixed columns (1-based):
        //   1- 3  "001"
        //   5- 8  starting rank (right-justified)
        //  10     sex (m/w/space)
        //  11-13  title (3 chars)
        //  15-47  name (33 chars left-justified)
        //  49-52  rating (4 chars right-justified)
        //  54-56  federation (3 chars)
        //  58-68  FIDE / local ID (11 chars right-justified)
        //  70-79  birth date YYYY/MM/DD (10 chars)
        //  81-84  points "X.Y" (right-justified in 4 chars)
        //  86-89  final rank (4 chars, blank if not yet determined)
        //  91...  per-round blocks (10 chars wide: 4 opp + space + color + space + result + space)
        var sb = new StringBuilder(200);

        Append(sb, "001", width: 3);                                         //  1-3
        sb.Append(' ');                                                      //  4
        Append(sb, player.PairNumber.ToString(CultureInfo.InvariantCulture),
            width: 4, rightJustify: true);                                   //  5-8
        sb.Append(' ');                                                      //  9
        sb.Append(' ');                                                      // 10  sex (unknown)
        Append(sb, string.Empty, width: 3);                                  // 11-13  title
        sb.Append(' ');                                                      // 14
        Append(sb, SafeName(player.Name), width: 33);                        // 15-47
        sb.Append(' ');                                                      // 48
        Append(sb, player.Rating.ToString(CultureInfo.InvariantCulture),
            width: 4, rightJustify: true);                                   // 49-52
        sb.Append(' ');                                                      // 53
        Append(sb, string.Empty, width: 3);                                  // 54-56  federation (unknown)
        sb.Append(' ');                                                      // 57
        Append(sb, player.UscfId ?? string.Empty, width: 11, rightJustify: true); // 58-68
        sb.Append(' ');                                                      // 69
        Append(sb, "          ", width: 10);                                 // 70-79  birth (unknown)
        sb.Append(' ');                                                      // 80
        Append(sb, FormatPoints(player.Score), width: 4, rightJustify: true);// 81-84
        sb.Append(' ');                                                      // 85
        Append(sb, string.Empty, width: 4);                                  // 86-89  final rank
        sb.Append(' ');                                                      // 90

        for (var r = 0; r < roundsPlayed; r++)
        {
            var history = r < player.History.Count ? player.History[r] : RoundResult.Empty;
            AppendRoundCell(sb, history);
        }

        // Do not trim: BBP's TRF parser expects every per-round cell to be
        // exactly 10 characters wide (including the trailing space), so the
        // line length must reach column 90 + 10*rounds. Stripping the final
        // space shortens the last cell to 9 chars and BBP reports an
        // "Invalid line" error when it tries to read past the end.
        return sb.ToString();
    }

    private static void AppendRoundCell(StringBuilder sb, RoundResult result)
    {
        // FIDE TRF-16 per-round block layout (10 chars wide):
        //   col N+0   : blank separator
        //   col N+1..4: opponent pair number (right-justified)
        //   col N+5   : blank
        //   col N+6   : colour ('w' / 'b' / '-')
        //   col N+7   : blank
        //   col N+8   : result code ('1' / '0' / '=' / 'U' / 'H' / '-')
        //   col N+9   : blank trailer
        // The very first round cell sits at columns 91-100 so the
        // opponent number starts at column 92, NOT column 91.
        var opponentField = result.Opponent > 0
            ? result.Opponent.ToString(CultureInfo.InvariantCulture)
            : "0000";

        var color = result.Color switch
        {
            PlayerColor.White => 'w',
            PlayerColor.Black => 'b',
            _ => '-',
        };

        var code = result.Kind switch
        {
            RoundResultKind.Win          => '1',
            RoundResultKind.Loss         => '0',
            RoundResultKind.Draw         => '=',
            RoundResultKind.FullPointBye => 'U',   // FIDE: full-point bye
            RoundResultKind.HalfPointBye => 'H',   // FIDE: half-point bye
            RoundResultKind.None         => '-',
            _                            => '-',
        };

        // Byes and unpaired rounds have no opponent and no colour.
        if (!IsPlayedGame(result.Kind))
        {
            opponentField = "0000";
            color = '-';
        }

        sb.Append(' ');                                                  // N+0 blank separator
        Append(sb, opponentField, width: 4, rightJustify: true);         // N+1..4 opponent
        sb.Append(' ');                                                  // N+5
        sb.Append(color);                                                // N+6 colour
        sb.Append(' ');                                                  // N+7
        sb.Append(code);                                                 // N+8 result
        sb.Append(' ');                                                  // N+9 trailer
    }

    private static bool IsPlayedGame(RoundResultKind kind) =>
        kind is RoundResultKind.Win or RoundResultKind.Loss or RoundResultKind.Draw;

    private static string FormatPoints(decimal score) =>
        score.ToString("0.0", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly? date) =>
        date.HasValue ? date.Value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) : string.Empty;

    private static string SafeName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        // TRF is ASCII-only. Strip any non-ASCII characters conservatively.
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(c <= 0x7E && c >= 0x20 ? c : '?');
        }
        return sb.ToString();
    }

    private static void WriteLine(TextWriter w, string tag, string value)
    {
        w.Write(tag);
        w.Write(' ');
        w.WriteLine(value);
    }

    private static void Append(
        StringBuilder sb,
        string value,
        int width,
        bool rightJustify = false)
    {
        value ??= string.Empty;
        if (value.Length > width)
        {
            sb.Append(value, 0, width);
            return;
        }

        var padding = width - value.Length;
        if (rightJustify)
        {
            sb.Append(' ', padding);
            sb.Append(value);
        }
        else
        {
            sb.Append(value);
            sb.Append(' ', padding);
        }
    }
}
