using System;
using System.IO;

namespace FreePair.Core.Importers;

/// <summary>
/// Thin façade that dispatches a player-roster import to the
/// right format-specific importer based on the file extension. UI
/// code can call this without knowing which parser to pick.
/// </summary>
public static class PlayerImport
{
    public static PlayerImportResult FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".xlsx" => PlayerXlsxImporter.Import(path),
            _       => PlayerCsvImporter.Import(path),
        };
    }
}
