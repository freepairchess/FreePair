using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FreePair.Core.Uscf.Trf;

/// <summary>
/// Reads FIDE TRF documents — specifically the dialect emitted by FreePair's
/// <c>TrfWriter</c> (which is also what bbpPairings consumes). Designed as
/// a tolerant column-based parser: unknown tags are ignored, malformed
/// player lines are skipped (with the line number captured for error
/// surfacing if needed), and per-round cells past the line end fall back
/// to <see cref="TrfRoundCell.Empty"/>.
/// </summary>
/// <remarks>
/// <para>Recognised header tags:</para>
/// <list type="bullet">
///   <item><c>012</c> tournament name</item>
///   <item><c>042</c> start date / <c>052</c> end date</item>
///   <item><c>062</c> player count (informational only)</item>
///   <item><c>XXR</c> total rounds</item>
///   <item><c>XXC</c> initial colour (<c>white1</c> / <c>black1</c>)</item>
///   <item><c>001</c> per-player line (parsed by fixed columns)</item>
/// </list>
/// </remarks>
public static class TrfReader
{
    /// <summary>Parses the full TRF document text.</summary>
    public static TrfDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var name = string.Empty;
        var startDate = string.Empty;
        var endDate = string.Empty;
        int? totalRounds = null;
        char? initialColor = null;
        var players = new List<TrfPlayer>();

        var lines = text.Replace("\r\n", "\n").Split('\n');
        foreach (var rawLine in lines)
        {
            // Don't trim — TRF is column-based, leading whitespace matters.
            // We only strip the trailing newline / carriage return that
            // Split already removed.
            var line = rawLine.TrimEnd();
            if (line.Length == 0) continue;

            var tag = line.Length >= 3 ? line[..3] : line;

            switch (tag)
            {
                case "012":
                    name = ValueAfterTag(line);
                    break;
                case "042":
                    startDate = ValueAfterTag(line);
                    break;
                case "052":
                    endDate = ValueAfterTag(line);
                    break;
                case "XXR":
                    if (int.TryParse(ValueAfterTag(line), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out var r))
                    {
                        totalRounds = r;
                    }
                    break;
                case "XXC":
                    var v = ValueAfterTag(line).Trim().ToLowerInvariant();
                    if (v.StartsWith("white", StringComparison.Ordinal)) initialColor = 'w';
                    else if (v.StartsWith("black", StringComparison.Ordinal)) initialColor = 'b';
                    break;
                case "001":
                    if (TryParsePlayer(line, out var player))
                    {
                        players.Add(player);
                    }
                    break;
                default:
                    // Other TRF tags (022 city, 032 federation, 062 count,
                    // 092 type, 122 time control, 132 round dates, etc.)
                    // are not consumed by the pairing engine.
                    break;
            }
        }

        var resolvedRounds = totalRounds ?? InferRoundsFromPlayers(players);
        var requestedByes  = ExtractRequestedByes(players);
        return new TrfDocument(
            name, startDate, endDate, resolvedRounds, initialColor, players,
            RequestedByes: requestedByes.Count == 0 ? null : requestedByes);
    }

    /// <summary>
    /// Pulls upcoming-round half-point bye markers out of the parsed
    /// player history. The wrapper / writer convention is:
    /// <list type="bullet">
    ///   <item>Past rounds with an H cell are already-honoured byes
    ///         and stay in the player's history; we ignore them.</item>
    ///   <item>The cell at index <c>playedRounds</c> (0-based) — i.e.
    ///         the FIRST cell past the last actually-played round —
    ///         encodes a TD-pre-flagged half-point bye for the round
    ///         being paired. Players with that cell get added to the
    ///         document's <c>RequestedByes</c> dict so
    ///         <see cref="UscfPairer"/> filters them out of the
    ///         pairing pool.</item>
    /// </list>
    /// <para>Zero-point byes don't appear in the TRF at all (the
    /// writer filters those players out entirely), so this method
    /// only emits 'H' entries.</para>
    /// </summary>
    private static Dictionary<int, char> ExtractRequestedByes(IReadOnlyList<TrfPlayer> players)
    {
        // playedRounds = the maximum index + 1 across all players where
        // any cell records a real game (Opponent > 0). Anything past
        // that index in any player's row is "future" — and the only
        // future cell content the writer ever emits is the upcoming
        // half-point bye marker.
        var playedRounds = 0;
        foreach (var p in players)
        {
            for (var i = 0; i < p.Rounds.Count; i++)
            {
                if (p.Rounds[i].Opponent > 0)
                {
                    if (i + 1 > playedRounds) playedRounds = i + 1;
                }
            }
        }

        var requested = new Dictionary<int, char>();
        foreach (var p in players)
        {
            if (playedRounds >= p.Rounds.Count) continue;
            var cell = p.Rounds[playedRounds];
            if (cell.Opponent != 0) continue;
            // Result codes for TD-pre-flagged byes: 'H' = half-point,
            // 'Z' = zero-point (rarely emitted by the writer but
            // honour it if present).
            if (cell.Result == 'H')
            {
                requested[p.PairNumber] = 'H';
            }
            else if (cell.Result == 'Z')
            {
                requested[p.PairNumber] = 'Z';
            }
        }
        return requested;
    }

    private static string ValueAfterTag(string line) =>
        line.Length <= 4 ? string.Empty : line[4..].TrimStart();

    /// <summary>
    /// Parses a single TRF <c>001</c> player line by fixed columns. Returns
    /// <c>false</c> when the line is malformed (missing required fields).
    /// </summary>
    /// <remarks>
    /// Column layout (1-based, per FIDE TRF-16 / what FreePair's
    /// <c>TrfWriter</c> emits):
    /// <list type="bullet">
    ///   <item>5-8 starting rank</item>
    ///   <item>15-47 name</item>
    ///   <item>49-52 rating</item>
    ///   <item>58-68 USCF/FIDE id</item>
    ///   <item>81-84 points</item>
    ///   <item>91+ per-round cells, 10 chars wide</item>
    /// </list>
    /// </remarks>
    internal static bool TryParsePlayer(string line, out TrfPlayer player)
    {
        player = null!;

        if (line.Length < 8) return false;

        if (!int.TryParse(SafeSlice(line, 4, 4).Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var pair) || pair <= 0)
        {
            return false;
        }

        var name = SafeSlice(line, 14, 33).Trim();

        var ratingText = SafeSlice(line, 48, 4).Trim();
        var rating = 0;
        if (ratingText.Length > 0)
        {
            int.TryParse(ratingText, NumberStyles.Integer, CultureInfo.InvariantCulture, out rating);
        }

        var id = SafeSlice(line, 57, 11).Trim();

        var pointsText = SafeSlice(line, 80, 4).Trim();
        decimal points = 0m;
        if (pointsText.Length > 0)
        {
            decimal.TryParse(pointsText, NumberStyles.Number, CultureInfo.InvariantCulture, out points);
        }

        var rounds = ParseRoundCells(line);

        player = new TrfPlayer(pair, name, rating, id, points, rounds);
        return true;
    }

    private static IReadOnlyList<TrfRoundCell> ParseRoundCells(string line)
    {
        // Round cells start at column 91 (offset 90), each 10 chars wide.
        const int RoundsStart = 90;
        const int CellWidth = 10;

        if (line.Length <= RoundsStart) return Array.Empty<TrfRoundCell>();

        var cells = new List<TrfRoundCell>();
        for (var offset = RoundsStart; offset + CellWidth <= line.Length + 1; offset += CellWidth)
        {
            // Each cell layout: " OOOO C R " — 1 separator + 4 opp + 1 + colour + 1 + result + 1.
            // We accept slightly truncated trailing cells (no trailing space) since
            // some writers strip whitespace; require at least the result column.
            if (offset + 9 > line.Length)
            {
                // Trailing cell exists but is shorter than expected — treat as empty.
                break;
            }

            var oppText = SafeSlice(line, offset + 1, 4).Trim();
            var color = offset + 6 < line.Length ? line[offset + 6] : '-';
            var result = offset + 8 < line.Length ? line[offset + 8] : '-';

            int opp = 0;
            if (oppText.Length > 0)
            {
                int.TryParse(oppText, NumberStyles.Integer, CultureInfo.InvariantCulture, out opp);
            }

            // Empty / fully blank cell (all spaces) — stop scanning to avoid
            // accreting trailing whitespace as fake round data.
            if (opp == 0 && (color == ' ' || color == '-') && (result == ' ' || result == '-'))
            {
                if (oppText.Length == 0)
                {
                    break;
                }
            }

            // Normalise colour / result to canonical glyphs.
            if (color == ' ') color = '-';
            if (result == ' ') result = '-';

            cells.Add(new TrfRoundCell(opp, color, result));
        }

        return cells;
    }

    private static int InferRoundsFromPlayers(IReadOnlyList<TrfPlayer> players)
    {
        if (players.Count == 0) return 0;
        var maxHistory = players.Max(p => p.Rounds.Count);
        // If we have N rounds of history, the next round to pair is N+1; the
        // engine wants to know the total rounds-in-tournament. Without an
        // XXR directive we err on the side of "pair one more round than
        // we've seen".
        return maxHistory + 1;
    }

    private static string SafeSlice(string line, int start, int length)
    {
        if (start >= line.Length) return string.Empty;
        var end = Math.Min(line.Length, start + length);
        return line.Substring(start, end - start);
    }
}
