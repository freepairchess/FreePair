using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.Importers;

namespace FreePair.Core.Tests.Importers;

/// <summary>
/// Tests for <see cref="PlayerCsvImporter"/> and
/// <see cref="PlayerXlsxImporter"/> — header alias resolution,
/// quoted-cell escaping, warnings, and full end-to-end against the
/// committed sample files in <c>docs/SampleImports/</c>.
/// </summary>
public class PlayerImportTests
{
    private static string SamplePath(string filename)
    {
        // Samples live at the repo root so they can be handed to
        // end-users. Walk up from the test binary's directory until
        // we find docs/SampleImports next to the .git root.
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "docs", "SampleImports")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "docs", "SampleImports", filename);
    }

    [Fact]
    public void Csv_parses_all_rows_and_resolves_aliases()
    {
        var path = SamplePath("players.csv");
        var result = PlayerImport.FromFile(path);

        Assert.True(result.Players.Count >= 5, $"expected ≥5 rows, got {result.Players.Count}");
        var alice = result.Players.First(p => p.Name.StartsWith("Alice"));
        Assert.Equal(1820, alice.Rating);
        Assert.Equal("12345678", alice.UscfId);
        Assert.Equal("Seattle CC", alice.Club);
        Assert.Equal("WA", alice.State);
        Assert.Equal("Knights", alice.Team);
        Assert.Equal("alice@example.com", alice.Email);
    }

    [Fact]
    public void Csv_honours_quoted_cells_with_embedded_quotes()
    {
        var path = SamplePath("players.csv");
        var result = PlayerImport.FromFile(path);
        var dave = result.Players.First(p => p.Name.Contains("Daniels"));
        Assert.Equal(@"Dave ""Doc"" Daniels", dave.Name);
    }

    [Fact]
    public void Csv_unparseable_rating_defaults_to_zero_with_warning()
    {
        var path = SamplePath("players.csv");
        var result = PlayerImport.FromFile(path);
        var frank = result.Players.First(p => p.Name.StartsWith("Frank"));
        Assert.Equal(0, frank.Rating);
        Assert.Contains(result.Warnings, w => w.Contains("Frank") && w.Contains("unrated"));
    }

    [Fact]
    public void Tsv_produces_equivalent_output_to_csv()
    {
        var csv = PlayerImport.FromFile(SamplePath("players.csv"));
        var tsv = PlayerImport.FromFile(SamplePath("players.tsv"));
        Assert.Equal(csv.Players.Count, tsv.Players.Count);
        // Name + rating should match row-for-row (contact detail
        // normalization already proven by the CSV tests above).
        for (var i = 0; i < csv.Players.Count; i++)
        {
            // Dave Daniels has quoted alias in CSV, plain in TSV — skip him.
            if (csv.Players[i].Name.Contains("Daniels")) continue;
            Assert.Equal(csv.Players[i].Name, tsv.Players[i].Name);
            Assert.Equal(csv.Players[i].Rating, tsv.Players[i].Rating);
        }
    }

    [Fact]
    public void Csv_reader_reports_missing_name_column()
    {
        using var r = new StringReader("Rating,Club\n1500,Seattle\n");
        var result = PlayerCsvImporter.Import(r, ',');
        Assert.Empty(result.Players);
        Assert.Contains(result.Warnings, w => w.Contains("Name"));
    }

    [Fact]
    public void Csv_reader_skips_blank_rows_and_blank_names()
    {
        using var r = new StringReader("Name,Rating\nAlice,1500\n\n,1600\nBob,1700\n");
        var result = PlayerCsvImporter.Import(r, ',');
        Assert.Equal(2, result.Players.Count);
        Assert.Contains(result.Warnings, w => w.Contains("blank name"));
    }

    [Fact]
    public void Xlsx_round_trips_the_same_rows_as_csv()
    {
        // Generate a .xlsx from the CSV at test-time so we always
        // exercise the ClosedXML path without committing a binary
        // blob. Drop the workbook in the per-test temp dir.
        var csvPath = SamplePath("players.csv");
        var csv = PlayerImport.FromFile(csvPath);

        var tmp = Path.Combine(Path.GetTempPath(), $"fp-xlsx-{System.Guid.NewGuid():N}.xlsx");
        try
        {
            using (var wb = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = wb.AddWorksheet("Players");
                ws.Cell(1, 1).Value = "Name";
                ws.Cell(1, 2).Value = "Rating";
                ws.Cell(1, 3).Value = "USCF ID";
                ws.Cell(1, 4).Value = "Club";
                ws.Cell(1, 5).Value = "State";
                for (var i = 0; i < csv.Players.Count; i++)
                {
                    var p = csv.Players[i];
                    ws.Cell(i + 2, 1).Value = p.Name;
                    ws.Cell(i + 2, 2).Value = p.Rating;
                    ws.Cell(i + 2, 3).Value = p.UscfId;
                    ws.Cell(i + 2, 4).Value = p.Club;
                    ws.Cell(i + 2, 5).Value = p.State;
                }
                wb.SaveAs(tmp);
            }

            var xlsx = PlayerImport.FromFile(tmp);
            Assert.Equal(csv.Players.Count, xlsx.Players.Count);
            Assert.Equal(csv.Players[0].Name, xlsx.Players[0].Name);
            Assert.Equal(csv.Players[0].Rating, xlsx.Players[0].Rating);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
