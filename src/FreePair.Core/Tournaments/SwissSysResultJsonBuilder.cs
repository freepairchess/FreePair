using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments.Standings;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Emits the NAChessHub "results / pairings display" JSON derived
/// from a <see cref="Tournament"/>. NAChessHub's own SwissSys desktop
/// upload path authors a richer version of this file that also carries
/// TD-local values (short account id, short passcode, TD name, bye
/// message, default city) — none of which the <c>.sjson</c> carries.
/// FreePair does a best-effort derivation using only what the domain
/// model has; missing TD-local fields are emitted as empty strings or
/// <see langword="null"/>. The hub still renders pairings / results /
/// standings correctly because those come entirely from the derived
/// section payload.
/// </summary>
/// <remarks>
/// Output shape (root keys):
/// <code>
/// {
///   "NACHEventID":       string  (Tournament.NachEventId — GUID or empty)
///   "NACHEventPasscode": string  (Tournament.NachPasscode — GUID or empty)
///   "tournament":        string
///   "date":              "yyyy/M/d"
///   "city", "state", "country": strings
///   "td":                ""  (not in .sjson)
///   "bye_message":       ""  (not in .sjson)
///   "sections": [{
///     "section", "rounds_paired", "rounds_played", "time_stamp",
///     "standings": [pairNumber],
///     "place": ["1","2-3",…],
///     "tiebreaks": ["Mod. Med","Solk","Cumul.","Op. cumul."],
///     "rounds": [{ "pairings": [{ "white","black","board","result" }] }],
///     "players": [{ name,id1,rating,rating2,club,state?,byes?,tiebreaks,
///                   results,colors,ops,boards }]
///   }]
/// }
/// </code>
/// </remarks>
public static class SwissSysResultJsonBuilder
{
    /// <summary>
    /// Builds the derived result JSON and returns it as an
    /// indented UTF-8 string suitable for writing to disk.
    /// </summary>
    public static string Build(Tournament tournament, DateTimeOffset? nowOverride = null)
    {
        ArgumentNullException.ThrowIfNull(tournament);

        var now = nowOverride ?? DateTimeOffset.Now;

        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();

            WriteString(w, "NACHEventID",       tournament.NachEventId  ?? "");
            WriteString(w, "NACHEventPasscode", tournament.NachPasscode ?? "");
            WriteString(w, "tournament",        tournament.Title        ?? "");
            WriteString(w, "date",              FormatDate(tournament.StartDate));
            WriteString(w, "city",              tournament.EventCity    ?? "");
            WriteString(w, "state",             tournament.EventState   ?? "");
            WriteString(w, "country",           tournament.EventCountry ?? "");
            // TD-local fields we don't have — empty keeps the schema
            // shape identical to what NAChessHub produces itself.
            WriteString(w, "td",                "");
            WriteString(w, "bye_message",       "");

            w.WritePropertyName("sections");
            w.WriteStartArray();
            foreach (var section in tournament.Sections)
            {
                WriteSection(w, section, now);
            }
            w.WriteEndArray();

            w.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    // ========================================================================
    // section
    // ========================================================================

    private static void WriteSection(Utf8JsonWriter w, Section section, DateTimeOffset now)
    {
        w.WriteStartObject();
        WriteString(w, "section",       section.Name);
        w.WriteNumber("rounds_paired",  section.RoundsPaired);
        w.WriteNumber("rounds_played",  section.RoundsPlayed);
        WriteString(w, "time_stamp",    FormatTimeStamp(now));

        // ---- standings (pair numbers) + place labels ----
        var standings = StandingsBuilder.Build(section);

        // NAChessHub's wire format uses 1-based INDEXES INTO the
        // standings array (not raw pair numbers) for the
        // rounds[].pairings[].white / .black fields. Pre-compute the
        // lookup once per section so emission is O(1) per pairing.
        // Pair number 0 means "no opponent" (solo bye) and must stay 0.
        var pairToStandingsIndex = new Dictionary<int, int>(standings.Count);
        for (var i = 0; i < standings.Count; i++)
        {
            pairToStandingsIndex[standings[i].PairNumber] = i + 1;
        }

        w.WritePropertyName("standings");
        w.WriteStartArray();
        foreach (var row in standings) w.WriteNumberValue(row.PairNumber);
        w.WriteEndArray();

        w.WritePropertyName("place");
        w.WriteStartArray();
        foreach (var row in standings) w.WriteStringValue(row.Place);
        w.WriteEndArray();

        // ---- tiebreak system display names ----
        // Swiss sections get the fixed USCF 4-tiebreak set. Round-robin
        // sections don't use those tiebreaks (rank is decided by raw
        // score + Sonneborn-Berger, which the hub renders separately);
        // NAChessHub's own RR output uses an empty array here, so we
        // mirror that.
        w.WritePropertyName("tiebreaks");
        w.WriteStartArray();
        if (section.Kind != SectionKind.RoundRobin)
        {
            w.WriteStringValue("Mod. Med");
            w.WriteStringValue("Solk");
            w.WriteStringValue("Cumul.");
            w.WriteStringValue("Op. cumul.");
        }
        w.WriteEndArray();

        // ---- rounds → pairings ----
        w.WritePropertyName("rounds");
        w.WriteStartArray();
        foreach (var round in section.Rounds)
        {
            WriteRound(w, round, section, pairToStandingsIndex);
        }
        w.WriteEndArray();

        // ---- players ----
        w.WritePropertyName("players");
        w.WriteStartArray();
        // Standings order mirrors what the hub displays as the
        // leaderboard; emit the player array in that same order so
        // `standings[i]` and the i-th entry in `players` align.
        foreach (var row in standings)
        {
            var player = section.Players.First(p => p.PairNumber == row.PairNumber);
            WritePlayer(w, player, section, row);
        }
        w.WriteEndArray();

        w.WriteEndObject();
    }

    // ========================================================================
    // rounds / pairings
    // ========================================================================

    private static void WriteRound(
        Utf8JsonWriter w, Round round, Section section,
        IReadOnlyDictionary<int, int> pairToStandingsIndex)
    {
        w.WriteStartObject();
        w.WritePropertyName("pairings");
        w.WriteStartArray();

        // Head-to-head pairings first (board > 0 in the domain).
        foreach (var p in round.Pairings.OrderBy(p => p.Board))
        {
            w.WriteStartObject();
            // Emit white / black as 1-based standings indexes
            // (NAChessHub's wire convention), not raw pair numbers.
            w.WriteNumber("white",  StandingsIndex(pairToStandingsIndex, p.WhitePair));
            w.WriteNumber("black",  StandingsIndex(pairToStandingsIndex, p.BlackPair));
            w.WriteNumber("board",  p.Board);
            WriteString(w, "result", FormatTwoPlayerResult(p.Result));
            w.WriteEndObject();
        }

        // Solo entries (byes / unpaired withdrawals) after the head-to-heads.
        // Convention: white = standings index, black = 0, board = 0.
        foreach (var bye in round.Byes)
        {
            w.WriteStartObject();
            w.WriteNumber("white", StandingsIndex(pairToStandingsIndex, bye.PlayerPair));
            w.WriteNumber("black", 0);
            w.WriteNumber("board", 0);
            WriteString(w, "result", FormatSoloResult(bye.Kind));
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    /// <summary>
    /// Converts a domain pair number to NAChessHub's 1-based standings
    /// index. Pair 0 (no opponent / solo bye) stays 0. Pair numbers not
    /// present in the standings (shouldn't happen; belt-and-braces) also
    /// map to 0.
    /// </summary>
    private static int StandingsIndex(IReadOnlyDictionary<int, int> lookup, int pairNumber)
    {
        if (pairNumber <= 0) return 0;
        return lookup.TryGetValue(pairNumber, out var idx) ? idx : 0;
    }

    private static string FormatTwoPlayerResult(PairingResult r) => r switch
    {
        PairingResult.WhiteWins => "(1-0)",
        PairingResult.BlackWins => "(0-1)",
        PairingResult.Draw      => "(0.5-0.5)",
        _                       => "",  // Unplayed: blank matches SwissSys output
    };

    private static string FormatSoloResult(ByeKind kind) => kind switch
    {
        ByeKind.Full     => "(1)",
        ByeKind.Half     => "(0.5)",
        ByeKind.Unpaired => "(0)",
        _                => "(0)",
    };

    // ========================================================================
    // players
    // ========================================================================

    private static void WritePlayer(Utf8JsonWriter w, Player p, Section section, StandingsRow row)
    {
        w.WriteStartObject();

        WriteString(w, "name",   p.Name);
        WriteString(w, "id1",    p.UscfId ?? "");
        w.WriteNumber("rating",  p.Rating);
        w.WriteNumber("rating2", p.SecondaryRating ?? 0);
        WriteString(w, "club",   p.Club  ?? "");
        if (!string.IsNullOrEmpty(p.State))
            WriteString(w, "state", p.State);

        // Comma-separated requested-bye round list, e.g. "5" or "6,7".
        if (p.RequestedByeRounds.Count > 0)
        {
            WriteString(w, "byes", string.Join(",", p.RequestedByeRounds));
        }

        w.WritePropertyName("tiebreaks");
        w.WriteStartArray();
        w.WriteNumberValue(row.Tiebreaks.ModifiedMedian);
        w.WriteNumberValue(row.Tiebreaks.Solkoff);
        w.WriteNumberValue(row.Tiebreaks.Cumulative);
        w.WriteNumberValue(row.Tiebreaks.OpponentCumulative);
        w.WriteEndArray();

        // results / colors / ops / boards: per-round parallel arrays.
        // Length = rounds_paired (NOT rounds_played) — NAChessHub's
        // Razor templates iterate `rounds_played + 1..rounds_paired`
        // when showing upcoming-round pairings, and index straight
        // into these arrays. Emitting only up to rounds_played throws
        // ArgumentOutOfRangeException on the server.
        var roundCount = Math.Min(section.RoundsPaired, p.History.Count);

        w.WritePropertyName("results");
        w.WriteStartArray();
        for (var i = 0; i < roundCount; i++)
            w.WriteStringValue(MapRoundResultLetter(p.History[i]));
        w.WriteEndArray();

        w.WritePropertyName("colors");
        w.WriteStartArray();
        for (var i = 0; i < roundCount; i++)
            w.WriteStringValue(MapColorLetter(p.History[i]));
        w.WriteEndArray();

        w.WritePropertyName("ops");
        w.WriteStartArray();
        for (var i = 0; i < roundCount; i++)
            w.WriteNumberValue(p.History[i].Opponent);
        w.WriteEndArray();

        w.WritePropertyName("boards");
        w.WriteStartArray();
        for (var i = 0; i < roundCount; i++)
            w.WriteNumberValue(p.History[i].Board);
        w.WriteEndArray();

        w.WriteEndObject();
    }

    /// <summary>
    /// Per-round result letter expected by NAChessHub. Slots with
    /// <see cref="RoundResultKind.None"/> (paired-unplayed AND unpaired)
    /// emit an empty string — the hub's template branches on a blank
    /// result letter to render just color + opponent for future rounds,
    /// or "~" for in-progress pairings.
    /// </summary>
    private static string MapRoundResultLetter(RoundResult r) => r.Kind switch
    {
        RoundResultKind.Win          => "W",
        RoundResultKind.Loss         => "L",
        RoundResultKind.Draw         => "D",
        RoundResultKind.FullPointBye => "B",
        RoundResultKind.HalfPointBye => "H",
        _                            => "",  // unplayed / paired-unplayed / unpaired
    };

    private static string MapColorLetter(RoundResult r) => r.Color switch
    {
        PlayerColor.White => "W",
        PlayerColor.Black => "B",
        _                 => "-",
    };

    // ========================================================================
    // helpers
    // ========================================================================

    private static string FormatDate(DateOnly? d) =>
        d is null ? "" : $"{d.Value.Year}/{d.Value.Month}/{d.Value.Day}";

    private static string FormatTimeStamp(DateTimeOffset t) =>
        $"{t.Month}/{t.Day}/{t.Year} - {t.Hour:D2}:{t.Minute:D2}";

    private static void WriteString(Utf8JsonWriter w, string name, string value)
    {
        w.WritePropertyName(name);
        w.WriteStringValue(value);
    }
}
