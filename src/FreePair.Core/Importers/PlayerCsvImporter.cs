using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FreePair.Core.Importers;

/// <summary>
/// Plain-text tabular importer for player rosters — handles both
/// comma-separated (<c>.csv</c>) and tab-separated (<c>.tsv</c>)
/// files. Header row is required and case-insensitive; recognized
/// column aliases are resolved via <see cref="HeaderAliases"/>.
/// Unknown columns are ignored (with a warning).
/// </summary>
/// <remarks>
/// <para>This is a deliberately minimal CSV reader — it handles
/// quoted cells with embedded delimiters / quotes, but not line
/// breaks inside quoted cells. Chess roster data is almost always
/// single-line per record so this tradeoff is fine; if we ever
/// need full RFC 4180 support we can swap in a library parser.</para>
/// </remarks>
public static class PlayerCsvImporter
{
    /// <summary>
    /// Case-insensitive mapping from header string → canonical
    /// field name. The header row's cells are looked up here; any
    /// cell whose value isn't present is reported as an unknown
    /// column warning.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> HeaderAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // name
            ["name"] = "name",
            ["player"] = "name",
            ["player name"] = "name",
            ["full name"] = "name",
            // rating (primary)
            ["rating"] = "rating",
            ["uscf rating"] = "rating",
            ["regular rating"] = "rating",
            ["rtg"] = "rating",
            // rating (secondary)
            ["rating 2"] = "rating2",
            ["rating2"] = "rating2",
            ["secondary rating"] = "rating2",
            ["quick rating"] = "rating2",
            // id
            ["id"] = "id",
            ["uscf id"] = "id",
            ["uscf"] = "id",
            ["membership"] = "id",
            ["membership id"] = "id",
            // membership expiration
            ["exp"] = "exp",
            ["expires"] = "exp",
            ["expiration"] = "exp",
            ["membership exp"] = "exp",
            ["membership expiration"] = "exp",
            ["exp1"] = "exp",
            // club
            ["club"] = "club",
            // state
            ["state"] = "state",
            ["st"] = "state",
            // team
            ["team"] = "team",
            // email
            ["email"] = "email",
            ["e-mail"] = "email",
            // phone
            ["phone"] = "phone",
            ["tel"] = "phone",
            ["telephone"] = "phone",
        };

    /// <summary>
    /// Imports a player roster from <paramref name="path"/>. The
    /// delimiter defaults to the file-extension inferred one
    /// (<c>.csv</c> → comma, <c>.tsv</c> / <c>.txt</c> → tab);
    /// pass an explicit <paramref name="delimiter"/> to override.
    /// </summary>
    public static PlayerImportResult Import(string path, char? delimiter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var d = delimiter ?? InferDelimiter(path);
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return Import(reader, d);
    }

    /// <summary>
    /// Imports a player roster from a text reader, using
    /// <paramref name="delimiter"/>. Does not dispose the reader.
    /// </summary>
    public static PlayerImportResult Import(TextReader reader, char delimiter)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var warnings = new List<string>();
        var drafts = new List<PlayerImportDraft>();

        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            warnings.Add("File is empty or has no header row.");
            return new PlayerImportResult(drafts, warnings);
        }

        var rawHeaders = ParseLine(headerLine, delimiter).ToArray();
        // Map each column index → canonical field name. Unknown
        // columns stay null and are reported as a single aggregate
        // warning so the log doesn't explode on wide tables.
        var columnFields = new string?[rawHeaders.Length];
        var unknownCols = new List<string>();
        for (var i = 0; i < rawHeaders.Length; i++)
        {
            var header = rawHeaders[i].Trim();
            if (HeaderAliases.TryGetValue(header, out var canon))
            {
                columnFields[i] = canon;
            }
            else if (!string.IsNullOrWhiteSpace(header))
            {
                unknownCols.Add(header);
            }
        }
        if (unknownCols.Count > 0)
        {
            warnings.Add($"Ignoring unknown column(s): {string.Join(", ", unknownCols)}.");
        }

        var nameCol = Array.IndexOf(columnFields, "name");
        if (nameCol < 0)
        {
            warnings.Add("No 'Name' column found; import aborted.");
            return new PlayerImportResult(drafts, warnings);
        }

        var lineNumber = 1; // header was line 1
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue; // blank row

            var cells = ParseLine(line, delimiter).ToArray();
            var getCell = (string field) =>
            {
                var idx = Array.IndexOf(columnFields, field);
                return idx >= 0 && idx < cells.Length ? cells[idx].Trim() : string.Empty;
            };

            var name = getCell("name");
            if (string.IsNullOrWhiteSpace(name))
            {
                warnings.Add($"Line {lineNumber}: blank name, row skipped.");
                continue;
            }

            var ratingText = getCell("rating");
            int rating = 0;
            if (!string.IsNullOrWhiteSpace(ratingText)
                && !int.TryParse(ratingText, NumberStyles.Integer, CultureInfo.InvariantCulture, out rating))
            {
                warnings.Add($"Line {lineNumber} ({name}): rating '{ratingText}' is not a number; defaulted to 0.");
                rating = 0;
            }

            int? rating2 = null;
            var rating2Text = getCell("rating2");
            if (!string.IsNullOrWhiteSpace(rating2Text))
            {
                if (int.TryParse(rating2Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r2))
                {
                    rating2 = r2;
                }
                else
                {
                    warnings.Add($"Line {lineNumber} ({name}): secondary rating '{rating2Text}' is not a number; ignored.");
                }
            }

            drafts.Add(new PlayerImportDraft(
                Name: name,
                Rating: rating,
                UscfId: NullIfBlank(getCell("id")),
                SecondaryRating: rating2,
                MembershipExpiration: NullIfBlank(getCell("exp")),
                Club: NullIfBlank(getCell("club")),
                State: NullIfBlank(getCell("state")),
                Team: NullIfBlank(getCell("team")),
                Email: NullIfBlank(getCell("email")),
                Phone: NullIfBlank(getCell("phone"))));
        }

        return new PlayerImportResult(drafts, warnings);
    }

    private static char InferDelimiter(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".tsv" or ".txt" => '\t',
            _                => ',',
        };
    }

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// Parses one line of a delimited file, honouring <c>""</c>
    /// escaping inside quoted cells.
    /// </summary>
    private static IEnumerable<string> ParseLine(string line, char delimiter)
    {
        var cells = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"'); i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == delimiter)
                {
                    cells.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"' && sb.Length == 0)
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }
        cells.Add(sb.ToString());
        return cells;
    }
}
