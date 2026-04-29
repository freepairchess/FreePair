namespace FreePair.Core.Tournaments;

using FreePair.Core.Tournaments.Enums;

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
    bool? IncludeSectionDates = null)
{
    /// <summary>
    /// Maps a NAChessHub <see cref="TimeControlType"/> onto the
    /// USCF rating-system letter accepted by USCF report files.
    /// Returns <c>null</c> when no useful mapping exists (the
    /// caller should keep its current value, default to <c>'R'</c>,
    /// or prompt the TD).
    /// </summary>
    public static char? RatingSystemFromTimeControl(TimeControlType? tct) => tct switch
    {
        TimeControlType.Bullet            => 'B',
        TimeControlType.Blitz             => 'B',
        TimeControlType.Rapid             => 'Q',
        TimeControlType.Classical         => 'R',
        // Dual-rated rapid + classical maps to USCF "Dual" code.
        TimeControlType.RapidAndClassical => 'D',
        _                                 => null,
    };

    /// <summary>
    /// True when the event's <see cref="RatingType"/> includes a
    /// FIDE component — covers <c>FIDE</c>, <c>USCF_FIDE</c>,
    /// <c>CFC_FIDE</c>, <c>CFC_USCF_FIDE</c>, <c>USCF_FIDE_NW</c>,
    /// <c>CFC_FIDE_NW</c>, <c>USCF_CFC_FIDE_NW</c>. Detected by
    /// substring on the enum's name so any future FIDE-bearing
    /// composite picks up automatically.
    /// </summary>
    public static bool IsFideRated(RatingType? rt) =>
        rt is not null &&
        rt.Value.ToString().Contains("FIDE", System.StringComparison.OrdinalIgnoreCase);
}
