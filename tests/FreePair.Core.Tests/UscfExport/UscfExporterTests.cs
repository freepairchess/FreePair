using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.UscfExport;

namespace FreePair.Core.Tests.UscfExport;

/// <summary>
/// Round-trips the Apr 2026 sample tournament through
/// <see cref="UscfExporter"/> and compares the generated DBFs
/// field-by-field with the reference files SwissSys 11 produced
/// from the same .sjson. Catches schema drift (field count /
/// types / lengths / order), record-content bugs (round result
/// encoding, name truncation, etc.), and DBF header bugs (record
/// count, header length, record length).
/// </summary>
public class UscfExporterTests
{
    private const string FixtureFolder = "tests/FreePair.Core.Tests/Data/UscfExport/Apr2026";
    private const string SjsonName = "Chess_A2Z_April_Open_2026.sjson";

    /// <summary>
    /// Mirrors the H_* metadata SwissSys baked into the reference
    /// THEXPORT.DBF for the Apr 2026 sample so we can compare
    /// byte-for-byte. ProgramTag is matched to "SWISSSYS11" for
    /// the same reason — production exports tag themselves as
    /// "FREEPAIR".
    /// </summary>
    private static UscfExportOptions Apr2026Options() => new(
        AffiliateId:    "A4000429",
        City:           "Portland",
        State:          "OR",
        ZipCode:        "97225",
        Country:        "USA",
        ChiefTdId:      "16097497",
        AssistantTdId:  "16097502",
        SendCrossTable: 'N',
        RatingSystem:   'R',
        GrandPrix:      'N',
        FideRated:      'N');

    private static string FixturePath(string fileName) =>
        Path.Combine(TestPaths.RepoRoot, FixtureFolder, fileName);

    private static async Task<Tournament> LoadFixtureTournamentAsync()
    {
        var raw = await new SwissSysImporter().ImportAsync(FixturePath(SjsonName));
        return SwissSysMapper.Map(raw);
    }

    private static (string outFolder, string prefix) ExportToTemp(Tournament t, string programTag)
    {
        var folder = Path.Combine(Path.GetTempPath(), $"fp-uscf-{Guid.NewGuid():N}");
        var exporter = new UscfExporter { ProgramTag = programTag };
        exporter.Export(t, Apr2026Options(), folder, filePrefix: string.Empty);
        return (folder, string.Empty);
    }

    [Fact]
    public async Task Header_dbf_matches_swisssys_reference_byte_for_byte()
    {
        var t = await LoadFixtureTournamentAsync();
        var (folder, _) = ExportToTemp(t, programTag: "SWISSSYS11");
        try
        {
            var generated = File.ReadAllBytes(Path.Combine(folder, "THEXPORT.DBF"));
            var reference = File.ReadAllBytes(FixturePath("THEXPORT.DBF"));
            AssertDbfStructurallyEquivalent(generated, reference, "THEXPORT.DBF");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Sections_dbf_matches_swisssys_reference_field_by_field()
    {
        var t = await LoadFixtureTournamentAsync();
        var (folder, _) = ExportToTemp(t, programTag: "SWISSSYS11");
        try
        {
            var generated = File.ReadAllBytes(Path.Combine(folder, "TSEXPORT.DBF"));
            var reference = File.ReadAllBytes(FixturePath("TSEXPORT.DBF"));
            AssertDbfStructurallyEquivalent(generated, reference, "TSEXPORT.DBF");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Details_dbf_matches_swisssys_reference_field_by_field()
    {
        var t = await LoadFixtureTournamentAsync();
        var (folder, _) = ExportToTemp(t, programTag: "SWISSSYS11");
        try
        {
            var generated = File.ReadAllBytes(Path.Combine(folder, "TDEXPORT.DBF"));
            var reference = File.ReadAllBytes(FixturePath("TDEXPORT.DBF"));
            AssertDbfStructurallyEquivalent(generated, reference, "TDEXPORT.DBF");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Export_uses_file_prefix_for_all_three_files()
    {
        var t = await LoadFixtureTournamentAsync();
        var folder = Path.Combine(Path.GetTempPath(), $"fp-uscf-{Guid.NewGuid():N}");
        try
        {
            var paths = new UscfExporter().Export(t, Apr2026Options(), folder, "myevent_");
            Assert.Equal(3, paths.Count);
            Assert.True(File.Exists(Path.Combine(folder, "myevent_THEXPORT.DBF")));
            Assert.True(File.Exists(Path.Combine(folder, "myevent_TSEXPORT.DBF")));
            Assert.True(File.Exists(Path.Combine(folder, "myevent_TDEXPORT.DBF")));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    // ============ DBF-aware comparison ============

    /// <summary>
    /// Compares two DBF files at the structural level (record count,
    /// header layout, field schema, record bytes) — not byte-equal
    /// because the file's "last update" header bytes differ
    /// depending on when each file was generated. Reports the first
    /// mismatch with field-level context so test failures point
    /// straight at the offending column.
    /// </summary>
    private static void AssertDbfStructurallyEquivalent(byte[] gen, byte[] reference, string label)
    {
        Assert.True(gen.Length > 0, $"{label}: generated file is empty");
        Assert.Equal(reference[0], gen[0]);                  // version byte
        Assert.Equal(BitConverter.ToUInt32(reference, 4),
                     BitConverter.ToUInt32(gen, 4));         // record count
        var hdr = BitConverter.ToUInt16(reference, 8);
        var rl  = BitConverter.ToUInt16(reference, 10);
        Assert.Equal(hdr, BitConverter.ToUInt16(gen, 8));    // header length
        Assert.Equal(rl,  BitConverter.ToUInt16(gen, 10));   // record length

        // Field descriptors should match exactly.
        var nFields = (hdr - 33) / 32;
        for (var i = 0; i < nFields; i++)
        {
            var off = 32 + i * 32;
            var refName = Encoding.ASCII.GetString(reference, off, 11).TrimEnd('\0');
            var genName = Encoding.ASCII.GetString(gen, off, 11).TrimEnd('\0');
            Assert.Equal(refName, genName);
            Assert.Equal((char)reference[off + 11], (char)gen[off + 11]); // type
            Assert.Equal(reference[off + 16], gen[off + 16]);             // length
        }

        // Each record's content (after the 1-byte deletion flag).
        var recordCount = BitConverter.ToUInt32(reference, 4);
        for (var r = 0; r < recordCount; r++)
        {
            var rowOff = hdr + r * rl;
            var refRow = Encoding.ASCII.GetString(reference, rowOff, rl);
            var genRow = Encoding.ASCII.GetString(gen, rowOff, rl);
            Assert.Equal(refRow, genRow);
        }

        // EOF marker (final byte after last record).
        Assert.Equal(reference[^1], gen[^1]);
        Assert.Equal((byte)0x1A, gen[^1]);
    }
}
