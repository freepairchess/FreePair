using System;

namespace FreePair.Core.UscfExport;

/// <summary>
/// All event-level metadata that USCF needs but isn't carried in
/// the SwissSys <c>.sjson</c> file. The TD supplies these via the
/// export dialog (with sensible defaults pre-filled from
/// <c>AppSettings</c>). Mirrors the THEXPORT.DBF schema 1:1 for the
/// fields that aren't derived from the tournament model.
/// </summary>
/// <param name="UscfEventId">
/// USCF-assigned event ID. Blank on first submission — USCF fills
/// it in after the report is processed. 12 chars max.
/// </param>
/// <param name="AffiliateId">
/// Sponsoring affiliate ID, e.g. <c>"A4000429"</c>. 8 chars max.
/// </param>
/// <param name="City">21 chars max.</param>
/// <param name="State">2-letter postal code.</param>
/// <param name="ZipCode">10 chars max.</param>
/// <param name="Country">21 chars max. Defaults to <c>"USA"</c>.</param>
/// <param name="ChiefTdId">Chief TD's USCF ID. 8 chars max.</param>
/// <param name="AssistantTdId">Assistant TD's USCF ID. 8 chars max.</param>
/// <param name="OtherTdNotes">Free-form list of other TDs. 254 chars max.</param>
/// <param name="SendCrossTable">
/// <c>'Y'</c> to ask USCF for a cross-table back; <c>'N'</c> otherwise.
/// </param>
/// <param name="RatingSystem">
/// Per-section rating system letter — <c>'R'</c> = Regular,
/// <c>'Q'</c> = Quick, <c>'B'</c> = Blitz, <c>'D'</c> = Dual-rated.
/// Currently a single value applies to every section; future work
/// can lift this to a per-section override map.
/// </param>
/// <param name="GrandPrix">'Y' / 'N'.</param>
/// <param name="Scholastic">'Y' / 'N' / 'C' (collegiate).</param>
    /// <param name="FideRated">'Y' / 'N'.</param>
/// <param name="IncludeSectionDates">
/// When <c>true</c>, the per-section <c>S_BEG_DATE</c> /
/// <c>S_END_DATE</c> columns are filled with the tournament-level
/// start / end dates. When <c>false</c> (default), they're left
/// blank — both behaviours occur in real SwissSys exports.
/// </param>
public sealed record UscfExportOptions(
    string UscfEventId = "",
    string AffiliateId = "",
    string City = "",
    string State = "",
    string ZipCode = "",
    string Country = "USA",
    string ChiefTdId = "",
    string AssistantTdId = "",
    string OtherTdNotes = "",
    char SendCrossTable = 'N',
    char RatingSystem = 'R',
    char GrandPrix = 'N',
    char Scholastic = ' ',
    char FideRated = 'N',
    bool IncludeSectionDates = false)
{
    /// <summary>
    /// Hardens user-entered values to the lengths the DBF schemas
    /// expect — trims whitespace, truncates long strings, replaces
    /// nulls with empty. Returns a normalised copy.
    /// </summary>
    public UscfExportOptions Normalize() => this with
    {
        UscfEventId  = Trunc(UscfEventId,  12),
        AffiliateId  = Trunc(AffiliateId,  8),
        City         = Trunc(City,         21),
        State        = Trunc(State,        2),
        ZipCode      = Trunc(ZipCode,      10),
        Country      = Trunc(Country,      21),
        ChiefTdId    = Trunc(ChiefTdId,    8),
        AssistantTdId= Trunc(AssistantTdId,8),
        OtherTdNotes = Trunc(OtherTdNotes, 254),
    };

    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var t = s.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
