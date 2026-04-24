using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace FreePair.Core.Importers;

/// <summary>
/// Excel (<c>.xlsx</c>) player-roster importer. Reads the first
/// worksheet, assumes row 1 is a header, and reuses
/// <see cref="PlayerCsvImporter"/>'s header-alias recognition + row
/// parsing by converting the worksheet to in-memory tab-separated
/// lines first. Keeps the parsing rules (column aliases, required
/// name, rating defaults) identical across CSV / TSV / XLSX so TDs
/// can swap formats without changing the spreadsheet layout.
/// </summary>
public static class PlayerXlsxImporter
{
    public static PlayerImportResult Import(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var workbook = new XLWorkbook(path);
        var ws = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException($"Excel file '{path}' has no worksheets.");

        // Flatten to TSV in-memory. Cells are trimmed and any
        // embedded tab/newline is collapsed to a space so the
        // downstream line-oriented parser stays simple.
        var sb = new System.Text.StringBuilder();
        var range = ws.RangeUsed();
        if (range is null)
        {
            // Empty sheet — return an empty result with a warning.
            return new PlayerImportResult(
                System.Array.Empty<PlayerImportDraft>(),
                new[] { "Excel worksheet is empty." });
        }
        foreach (var row in range.Rows())
        {
            var cells = row.Cells().Select(c =>
            {
                var text = c.GetFormattedString() ?? string.Empty;
                return text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
            });
            sb.AppendLine(string.Join('\t', cells));
        }

        using var reader = new StringReader(sb.ToString());
        return PlayerCsvImporter.Import(reader, '\t');
    }
}
