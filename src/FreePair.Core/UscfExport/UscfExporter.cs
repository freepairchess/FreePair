using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.UscfExport;

/// <summary>
/// Builds the three <c>.DBF</c> files USCF accepts for tournament
/// rating submissions:
/// <list type="bullet">
///   <item><c>THEXPORT.DBF</c> — one row of event-level header data.</item>
///   <item><c>TSEXPORT.DBF</c> — one row per section.</item>
///   <item><c>TDEXPORT.DBF</c> — one row per player per section, with per-round result codes.</item>
/// </list>
/// SwissSys 11 produces the same three files; FreePair's exporter
/// matches its on-disk schema byte-for-byte (header bytes, field
/// descriptors, record layout, EOF marker) so the resulting files
/// drop straight into the USCF rater.
/// </summary>
public sealed class UscfExporter
{
    /// <summary>Identifier written into <c>H_PROGRAM</c>. Configurable for tests.</summary>
    public string ProgramTag { get; init; } = "FREEPAIR";

    /// <summary>
    /// Writes all three DBFs into <paramref name="folder"/>, with
    /// names prefixed by <paramref name="filePrefix"/> so multiple
    /// events' exports can coexist in the same directory. Returns
    /// the absolute paths of the files created.
    /// </summary>
    public IReadOnlyList<string> Export(
        Tournament tournament,
        UscfExportOptions options,
        string folder,
        string filePrefix)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(filePrefix);

        Directory.CreateDirectory(folder);
        var opts = options.Normalize();

        var header   = Path.Combine(folder, $"{filePrefix}THEXPORT.DBF");
        var sections = Path.Combine(folder, $"{filePrefix}TSEXPORT.DBF");
        var details  = Path.Combine(folder, $"{filePrefix}TDEXPORT.DBF");

        // Sections that don't have any players are skipped — same
        // behaviour SwissSys exhibits, and they wouldn't pass USCF
        // validation anyway.
        var liveSections = tournament.Sections
            .Where(s => s.Players.Count(p => !p.SoftDeleted) > 0)
            .ToArray();

        var maxRounds = liveSections.Length == 0
            ? 1
            : liveSections.Max(s => s.FinalRound);
        if (maxRounds < 1) maxRounds = 1;

        WriteHeader(header, tournament, liveSections.Length, opts);
        WriteSections(sections, liveSections, opts, tournament);
        WriteDetails(details, liveSections, opts, maxRounds);

        return new[] { header, sections, details };
    }

    // ============ THEXPORT ============

    private void WriteHeader(string path, Tournament t, int sectionCount, UscfExportOptions o)
    {
        var fields = new[]
        {
            new Dbf3Writer.Field("H_FORMAT",    5),
            new Dbf3Writer.Field("H_PROGRAM",   10),
            new Dbf3Writer.Field("H_EVENT_ID",  12),
            new Dbf3Writer.Field("H_NAME",      35),
            new Dbf3Writer.Field("H_TOT_SECT",  2),
            new Dbf3Writer.Field("H_BEG_DATE",  8),
            new Dbf3Writer.Field("H_END_DATE",  8),
            new Dbf3Writer.Field("H_AFF_ID",    8),
            new Dbf3Writer.Field("H_CITY",      21),
            new Dbf3Writer.Field("H_STATE",     2),
            new Dbf3Writer.Field("H_ZIPCODE",   10),
            new Dbf3Writer.Field("H_COUNTRY",   21),
            new Dbf3Writer.Field("H_SENDCROS",  1),
            new Dbf3Writer.Field("H_CTD_ID",    8),
            new Dbf3Writer.Field("H_ATD_ID",    8),
            new Dbf3Writer.Field("H_OTHER_TD",  254),
        };

        var row = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H_FORMAT"]   = "2C",
            ["H_PROGRAM"]  = ProgramTag,
            ["H_EVENT_ID"] = o.UscfEventId,
            ["H_NAME"]     = t.Title ?? string.Empty,
            ["H_TOT_SECT"] = sectionCount.ToString(CultureInfo.InvariantCulture),
            ["H_BEG_DATE"] = FormatDate(t.StartDate),
            ["H_END_DATE"] = FormatDate(t.EndDate),
            ["H_AFF_ID"]   = o.AffiliateId,
            ["H_CITY"]     = o.City,
            ["H_STATE"]    = o.State,
            ["H_ZIPCODE"]  = o.ZipCode,
            ["H_COUNTRY"]  = o.Country,
            ["H_SENDCROS"] = o.SendCrossTable.ToString(),
            ["H_CTD_ID"]   = o.ChiefTdId,
            ["H_ATD_ID"]   = o.AssistantTdId,
            ["H_OTHER_TD"] = o.OtherTdNotes,
        };

        var writer = new Dbf3Writer(fields);
        using var fs = File.Create(path);
        writer.Write(fs, new[] { row });
    }

    // ============ TSEXPORT ============

    private static void WriteSections(string path, IReadOnlyList<Section> sections, UscfExportOptions o, Tournament t)
    {
        var fields = new[]
        {
            new Dbf3Writer.Field("S_EVENT_ID", 12),
            new Dbf3Writer.Field("S_SEC_NUM",  2),
            new Dbf3Writer.Field("S_SEC_NAME", 30),
            new Dbf3Writer.Field("S_R_SYSTEM", 1),
            new Dbf3Writer.Field("S_TIMECTL",  40),
            new Dbf3Writer.Field("S_CTD_ID",   8),
            new Dbf3Writer.Field("S_ATD_ID",   8),
            new Dbf3Writer.Field("S_TRN_TYPE", 1),
            new Dbf3Writer.Field("S_TOT_RNDS", 2),
            new Dbf3Writer.Field("S_LST_PAIR", 4),
            new Dbf3Writer.Field("S_BEG_DATE", 8),
            new Dbf3Writer.Field("S_END_DATE", 8),
            new Dbf3Writer.Field("S_SCH_LVL",  1),
            new Dbf3Writer.Field("S_GR_PRIX",  1),
            new Dbf3Writer.Field("S_GP_PTS",   3),
            new Dbf3Writer.Field("S_FIDE",     1),
        };

        // SwissSys is inconsistent about per-section dates: some
        // exports leave them blank, others fill in the tournament-
        // level dates. We mirror that with an opt-in flag.
        var sectionBeg = o.IncludeSectionDates ? FormatDate(t.StartDate) : string.Empty;
        var sectionEnd = o.IncludeSectionDates ? FormatDate(t.EndDate)   : string.Empty;

        var rows = new List<IReadOnlyDictionary<string, string>>(sections.Count);
        for (var i = 0; i < sections.Count; i++)
        {
            var s = sections[i];
            var liveCount = s.Players.Count(p => !p.SoftDeleted);

            rows.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["S_EVENT_ID"] = o.UscfEventId,
                ["S_SEC_NUM"]  = (i + 1).ToString(CultureInfo.InvariantCulture),
                ["S_SEC_NAME"] = s.Name,
                ["S_R_SYSTEM"] = o.RatingSystem.ToString(),
                ["S_TIMECTL"]  = s.TimeControl ?? string.Empty,
                ["S_CTD_ID"]   = o.ChiefTdId,
                ["S_ATD_ID"]   = o.AssistantTdId,
                ["S_TRN_TYPE"] = SectionTypeLetter(s.Kind),
                ["S_TOT_RNDS"] = s.FinalRound.ToString(CultureInfo.InvariantCulture),
                ["S_LST_PAIR"] = liveCount.ToString(CultureInfo.InvariantCulture),
                ["S_BEG_DATE"] = sectionBeg,
                ["S_END_DATE"] = sectionEnd,
                ["S_SCH_LVL"]  = o.Scholastic.ToString(),
                ["S_GR_PRIX"]  = o.GrandPrix.ToString(),
                ["S_GP_PTS"]   = string.Empty,
                ["S_FIDE"]     = o.FideRated.ToString(),
            });
        }

        using var fs = File.Create(path);
        new Dbf3Writer(fields).Write(fs, rows);
    }

    // ============ TDEXPORT ============

    private static void WriteDetails(
        string path,
        IReadOnlyList<Section> sections,
        UscfExportOptions o,
        int maxRounds)
    {
        var staticFields = new List<Dbf3Writer.Field>
        {
            new("D_EVENT_ID", 12),
            new("D_SEC_NUM",  2),
            new("D_PAIR_NUM", 4),
            new("D_MEM_ID",   8),
            new("D_NAME",     30),
            new("D_STATE",    2),
            new("D_RATING",   4),
        };
        for (var r = 1; r <= maxRounds; r++)
        {
            staticFields.Add(new Dbf3Writer.Field($"D_RND{r:D2}", UscfRoundCode.Width));
        }

        var rows = new List<IReadOnlyDictionary<string, string>>();
        for (var i = 0; i < sections.Count; i++)
        {
            var s = sections[i];
            var sectionNum = (i + 1).ToString(CultureInfo.InvariantCulture);
            foreach (var p in s.Players.Where(p => !p.SoftDeleted))
            {
                var row = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["D_EVENT_ID"] = o.UscfEventId,
                    ["D_SEC_NUM"]  = sectionNum,
                    ["D_PAIR_NUM"] = p.PairNumber.ToString(CultureInfo.InvariantCulture),
                    ["D_MEM_ID"]   = p.UscfId ?? string.Empty,
                    ["D_NAME"]     = p.Name,
                    ["D_STATE"]    = p.State ?? string.Empty,
                    ["D_RATING"]   = p.Rating.ToString(CultureInfo.InvariantCulture),
                };

                for (var r = 1; r <= maxRounds; r++)
                {
                    string code;
                    if (r > s.FinalRound)
                    {
                        // Section played fewer rounds than the column count.
                        code = UscfRoundCode.Unplayed;
                    }
                    else if (r - 1 < p.History.Count)
                    {
                        code = UscfRoundCode.Encode(p.History[r - 1]);
                    }
                    else
                    {
                        // Player joined late / withdrew — round slot
                        // hasn't been stamped yet. USCF wants
                        // "U0     " for these gaps.
                        code = UscfRoundCode.Unplayed;
                    }
                    row[$"D_RND{r:D2}"] = code;
                }

                rows.Add(row);
            }
        }

        using var fs = File.Create(path);
        new Dbf3Writer(staticFields).Write(fs, rows);
    }

    // ============ helpers ============

    private static string FormatDate(DateOnly? d) =>
        d is null ? string.Empty : d.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    private static string SectionTypeLetter(SectionKind kind) => kind switch
    {
        SectionKind.Swiss      => "S",
        SectionKind.RoundRobin => "R",
        _                      => "S",
    };
}
