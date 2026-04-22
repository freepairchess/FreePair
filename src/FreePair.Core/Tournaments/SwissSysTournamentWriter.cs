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
