using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Default <see cref="ITournamentWriter"/> that round-trips edits back into
/// the originating SwissSys <c>.sjson</c> file.
/// </summary>
/// <remarks>
/// <para>The writer loads the existing file as a <see cref="JsonNode"/>
/// tree (preserving fields we do not model: prizes, seating options,
/// pair-table rows, overview metadata, etc.) and patches only the values
/// that reflect mutated tournament state:</para>
/// <list type="bullet">
///   <item>Each section's <c>Rounds paired</c> and <c>Rounds played</c>.</item>
///   <item>Each player's <c>Results</c> array, re-emitted from
///         <see cref="Player.History"/> via <see cref="RoundResult.ToSwissSysToken"/>.</item>
/// </list>
/// <para>Writes are atomic: a <c>.tmp</c> sibling is produced and then
/// moved over the target, so a crash mid-write cannot corrupt the user's
/// file.</para>
/// </remarks>
public class SwissSysTournamentWriter : ITournamentWriter
{
    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Indented = true,
    };

    public async Task SaveAsync(string filePath, Tournament tournament, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(tournament);

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

        var root = JsonNode.Parse(json)
            ?? throw new InvalidDataException($"File '{filePath}' is not valid JSON.");

        // ===== Overview / event-level metadata =====
        // Only keys whose domain value is non-null are patched. Null
        // values are treated as "leave alone" rather than "clear"
        // because we don't currently distinguish unset-by-loader from
        // actively-cleared-by-user. New keys that don't yet exist in
        // the source file are added.
        if (root["Overview"] is JsonObject overview)
        {
            PatchOverview(overview, tournament);
        }

        var sectionsArray = root["Sections"]?.AsArray()
            ?? throw new InvalidDataException("SwissSys file has no 'Sections' array.");

        foreach (var section in tournament.Sections)
        {
            var sectionNode = FindSectionByName(sectionsArray, section.Name);
            if (sectionNode is null)
            {
                // New section not present in the original file — skip; we
                // don't currently synthesize sections from thin air.
                continue;
            }

            sectionNode["Rounds paired"] = section.RoundsPaired;
            sectionNode["Rounds played"] = section.RoundsPlayed;

            var playersArray = sectionNode["Players"]?.AsArray();
            if (playersArray is null)
            {
                continue;
            }

            foreach (var player in section.Players)
            {
                var playerNode = FindPlayerByPairNumber(playersArray, player.PairNumber);
                if (playerNode is null)
                {
                    continue;
                }

                var resultsArray = new JsonArray();
                foreach (var entry in player.History)
                {
                    resultsArray.Add(JsonValue.Create(entry.ToSwissSysToken()));
                }

                playerNode["Results"] = resultsArray;
            }
        }

        await WriteAtomicAsync(filePath, root, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes each non-null event-level metadata value onto the raw
    /// top-level JSON object using the SwissSys 11 key names. Enum
    /// values use their C# member names (matching NAChessHub) so the
    /// file can round-trip through both systems.
    /// </summary>
    private static void PatchOverview(JsonObject overview, Tournament t)
    {
        SetIfSet(overview, "Tournament title",         t.Title);
        SetIfSet(overview, "Tournament time controls", t.TimeControl);
        SetIfSet(overview, "Starting date",            t.StartDate?.ToString("yyyy-MM-dd"));
        SetIfSet(overview, "Ending date",              t.EndDate?.ToString("yyyy-MM-dd"));
        SetIfSet(overview, "Starting date time",       t.StartDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"));
        SetIfSet(overview, "Ending date time",         t.EndDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"));
        SetIfSet(overview, "Time zone",                t.TimeZone);
        SetIfSet(overview, "Organizer ID",             t.OrganizerId);
        SetIfSet(overview, "Organizer ID Type",        t.OrganizerIdType?.ToString());
        SetIfSet(overview, "Organizer Name",           t.OrganizerName);
        SetIfSet(overview, "NACH passcode",            t.NachPasscode);

        SetIfSet(overview, "Event address",            t.EventAddress);
        SetIfSet(overview, "Event city",               t.EventCity);
        SetIfSet(overview, "Event state",              t.EventState);
        SetIfSet(overview, "Event zip code",           t.EventZipCode);
        SetIfSet(overview, "Event country",            t.EventCountry);

        SetIfSet(overview, "Event format",             t.EventFormat?.ToString());
        SetIfSet(overview, "Event type",               t.EventType?.ToString());
        SetIfSet(overview, "Pairing rule",             t.PairingRule?.ToString());
        SetIfSet(overview, "Time control type",        t.TimeControlType?.ToString());
        SetIfSet(overview, "Rating type",              t.RatingType?.ToString());

        if (t.RoundsPlanned is int r)           overview["Rounds"]          = r;
        if (t.HalfPointByesAllowed is int hb)   overview["Half point byes"] = hb;
        if (t.AutoPublishPairings is bool app)  overview["FreePair auto publish pairings"] = app;
        if (t.AutoPublishResults  is bool apr)  overview["FreePair auto publish results"]  = apr;
    }

    private static void SetIfSet(JsonObject o, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value)) o[key] = value;
    }

    private static JsonObject? FindSectionByName(JsonArray sections, string name)
    {
        foreach (var node in sections)
        {
            if (node is JsonObject obj &&
                string.Equals(obj["Section name"]?.GetValue<string>(), name, StringComparison.Ordinal))
            {
                return obj;
            }
        }
        return null;
    }

    private static JsonObject? FindPlayerByPairNumber(JsonArray players, int pairNumber)
    {
        foreach (var node in players)
        {
            if (node is JsonObject obj && TryGetInt(obj, "Pair number", out var n) && n == pairNumber)
            {
                return obj;
            }
        }
        return null;
    }

    private static bool TryGetInt(JsonObject obj, string key, out int value)
    {
        value = 0;
        var raw = obj[key];
        if (raw is null)
        {
            return false;
        }
        try
        {
            value = raw.GetValue<int>();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WriteAtomicAsync(string finalPath, JsonNode content, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(finalPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = ".";
        }

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(finalPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = File.Create(tempPath))
            await using (var writer = new Utf8JsonWriter(stream, s_writerOptions))
            {
                content.WriteTo(writer);
                await writer.FlushAsync(ct).ConfigureAwait(false);
            }

            File.Move(tempPath, finalPath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { /* best effort */ }
            throw;
        }
    }
}
