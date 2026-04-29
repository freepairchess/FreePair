namespace FreePair.Core.Tournaments;

/// <summary>
/// Per-tournament USCF report preferences. Persisted as
/// <c>"FreePair USCF *"</c> keys in the SwissSys Overview block,
/// so the values stick to the .sjson and the next export pre-fills
/// instantly without re-asking the TD. Distinct from the
/// app-wide <see cref="Settings.AppSettings"/> defaults: these
/// override per-event when present.
/// </summary>
/// <param name="AffiliateId">
/// Override for the affiliate id. When <c>null</c> the export
/// falls back to <c>Tournament.OrganizerId</c> (when its type is
/// <c>USCFAffiliateID</c>) or the app-wide default.
/// </param>
/// <param name="ChiefTdId">USCF id of the chief TD.</param>
/// <param name="AssistantTdId">USCF id of the assistant TD.</param>
/// <param name="OtherTdNotes">Free-form list of other TDs.</param>
/// <param name="RatingSystem">One letter — R/Q/B/D. <c>null</c> defaults to R.</param>
/// <param name="SendCrossTable">Y/N flag.</param>
/// <param name="GrandPrix">Y/N flag.</param>
/// <param name="FideRated">Y/N flag.</param>
/// <param name="IncludeSectionDates">
/// When <c>true</c>, the per-section S_BEG_DATE / S_END_DATE
/// columns are filled with the tournament dates. Both behaviours
/// occur in real SwissSys exports.
/// </param>
public sealed record UscfReportPrefs(
    string? AffiliateId = null,
    string? ChiefTdId = null,
    string? AssistantTdId = null,
    string? OtherTdNotes = null,
    char? RatingSystem = null,
    bool? SendCrossTable = null,
    bool? GrandPrix = null,
    bool? FideRated = null,
    bool? IncludeSectionDates = null);
