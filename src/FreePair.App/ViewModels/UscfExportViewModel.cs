using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Tournaments;
using FreePair.Core.UscfExport;
using FreePair.Core.Tournaments.Enums;
using System.Collections.Generic;
using System.Linq;

namespace FreePair.App.ViewModels;

/// <summary>
/// View-model backing the "Export USCF report files" dialog. Holds
/// the TD-supplied metadata that USCF requires but isn't carried
/// in the SwissSys <c>.sjson</c> file (CTD / ATD / affiliate IDs,
/// venue address, rating system, FIDE flag, etc.). On OK the
/// caller maps these into <see cref="UscfExportOptions"/> and
/// hands them to <see cref="UscfExporter"/>.
/// </summary>
public partial class UscfExportViewModel : ObservableObject
{
    [ObservableProperty] private string _uscfEventId   = string.Empty;
    [ObservableProperty] private string _affiliateId   = string.Empty;
    [ObservableProperty] private string _city          = string.Empty;
    [ObservableProperty] private string _state         = string.Empty;
    [ObservableProperty] private string _zipCode       = string.Empty;
    [ObservableProperty] private string _country       = "USA";
    [ObservableProperty] private string _chiefTdId     = string.Empty;
    [ObservableProperty] private string _assistantTdId = string.Empty;
    [ObservableProperty] private string _otherTdNotes  = string.Empty;

    [ObservableProperty] private bool _sendCrossTable;
    [ObservableProperty] private bool _grandPrix;
    [ObservableProperty] private bool _fideRated;
    [ObservableProperty] private bool _includeSectionDates;

    /// <summary>
    /// One letter — <c>R</c> regular, <c>Q</c> quick, <c>B</c>
    /// blitz, <c>D</c> dual-rated. UI binds to a combobox of these.
    /// </summary>
    [ObservableProperty] private string _ratingSystem = "R";

    [ObservableProperty] private string? _errorMessage;

    /// <summary>
    /// Validates required fields. Sets <see cref="ErrorMessage"/>
    /// on failure. Affiliate ID and Chief TD ID are required by
    /// USCF; everything else is best-effort.
    /// </summary>
    public bool TryValidate()
    {
        if (string.IsNullOrWhiteSpace(AffiliateId))
        {
            ErrorMessage = "Affiliate ID is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(ChiefTdId))
        {
            ErrorMessage = "Chief TD's USCF ID is required.";
            return false;
        }
        ErrorMessage = null;
        return true;
    }

    /// <summary>
    /// Materialises the dialog's fields into the Core-layer
    /// <see cref="UscfExportOptions"/> record (ready for
    /// <c>UscfExporter.Export</c>).
    /// </summary>
    public UscfExportOptions ToOptions() => new(
        UscfEventId:    UscfEventId.Trim(),
        AffiliateId:    AffiliateId.Trim(),
        City:           City.Trim(),
        State:          State.Trim().ToUpperInvariant(),
        ZipCode:        ZipCode.Trim(),
        Country:        string.IsNullOrWhiteSpace(Country) ? "USA" : Country.Trim(),
        ChiefTdId:      ChiefTdId.Trim(),
        AssistantTdId:  AssistantTdId.Trim(),
        OtherTdNotes:   OtherTdNotes.Trim(),
        SendCrossTable: SendCrossTable ? 'Y' : 'N',
        RatingSystem:   string.IsNullOrEmpty(RatingSystem) ? 'R' : RatingSystem[0],
        GrandPrix:      GrandPrix ? 'Y' : 'N',
        FideRated:      FideRated ? 'Y' : 'N',
        IncludeSectionDates: IncludeSectionDates);

    /// <summary>
    /// Maps the dialog state into the persisted per-tournament
    /// <see cref="UscfReportPrefs"/> so the next export pre-fills
    /// without re-asking. Distinct from <see cref="ToOptions"/>
    /// (which is the single-shot input to the exporter).
    /// </summary>
    public UscfReportPrefs ToPersistedPrefs() => new(
        AffiliateId:         AffiliateId.Trim(),
        ChiefTdId:           ChiefTdId.Trim(),
        AssistantTdId:       AssistantTdId.Trim(),
        OtherTdNotes:        OtherTdNotes.Trim(),
        RatingSystem:        string.IsNullOrEmpty(RatingSystem) ? 'R' : RatingSystem[0],
        SendCrossTable:      SendCrossTable,
        GrandPrix:           GrandPrix,
        FideRated:           FideRated,
        IncludeSectionDates: IncludeSectionDates);

    /// <summary>
    /// Pre-fills this VM with the right values for a tournament,
    /// using the priority cascade:
    /// <list type="number">
    /// <item>per-tournament USCF prefs (from a previous export);</item>
    /// <item>fields derived from the SwissSys Overview block —
    ///       Organizer ID (when type is USCFAffiliateID), Event
    ///       city/state/zip/country, Delegations (TDs first; else
    ///       the first delegation as chief);</item>
    /// <item>app-wide defaults from <c>AppSettings</c> (last-used).</item>
    /// </list>
    /// Higher-priority sources only fill fields the lower ones
    /// haven't already populated.
    /// </summary>
    public void Prefill(Tournament tournament, FreePair.Core.Settings.AppSettings appDefaults)
    {
        // Layer 3 (lowest): app-wide defaults.
        ChiefTdId     = appDefaults.UscfChiefTdId      ?? string.Empty;
        AssistantTdId = appDefaults.UscfAssistantTdId  ?? string.Empty;
        AffiliateId   = appDefaults.UscfAffiliateId    ?? string.Empty;
        City          = appDefaults.UscfCity           ?? string.Empty;
        State         = appDefaults.UscfState          ?? string.Empty;
        ZipCode       = appDefaults.UscfZipCode        ?? string.Empty;
        Country       = string.IsNullOrWhiteSpace(appDefaults.UscfCountry) ? "USA" : appDefaults.UscfCountry!;

        // Layer 2: derive from the tournament's Overview block.
        if (!string.IsNullOrWhiteSpace(tournament.EventCity))    City    = tournament.EventCity!;
        if (!string.IsNullOrWhiteSpace(tournament.EventState))   State   = tournament.EventState!;
        if (!string.IsNullOrWhiteSpace(tournament.EventZipCode)) ZipCode = tournament.EventZipCode!;
        if (!string.IsNullOrWhiteSpace(tournament.EventCountry)) Country = tournament.EventCountry!;

        // Affiliate id from Overview only when the organiser id
        // is actually a USCF affiliate id (otherwise it'd be a
        // FIDE / CFC org id which USCF would reject).
        if (tournament.OrganizerIdType == UserIDType.USCFAffiliateID &&
            !string.IsNullOrWhiteSpace(tournament.OrganizerId))
        {
            AffiliateId = tournament.OrganizerId!;
        }

        // Rating-system letter from the Time control type via the
        // Core helper. Anything else (Other / null) leaves the
        // previous value alone.
        if (UscfReportPrefs.RatingSystemFromTimeControl(tournament.TimeControlType) is char rsLetter)
        {
            RatingSystem = rsLetter.ToString();
        }

        // FIDE-rated flag: any Rating type whose enum name contains
        // "FIDE" qualifies. Doesn't auto-uncheck if the TD already
        // had it ticked — derivation is additive.
        if (UscfReportPrefs.IsFideRated(tournament.RatingType))
        {
            FideRated = true;
        }

        // Chief / Assistant TD from Delegations:
        //   - first entries with Level == TournamentDirector → Chief, Assistant
        //   - otherwise the first delegation (any level) → Chief, no Assistant
        if (tournament.Delegations is { Count: > 0 } dels)
        {
            var tds = dels.Where(d => d.Level == DelegationLevel.TournamentDirector).ToList();
            if (tds.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(tds[0].PlayerId)) ChiefTdId     = tds[0].PlayerId;
                if (tds.Count > 1 && !string.IsNullOrWhiteSpace(tds[1].PlayerId))
                    AssistantTdId = tds[1].PlayerId;
            }
            else if (!string.IsNullOrWhiteSpace(dels[0].PlayerId))
            {
                ChiefTdId = dels[0].PlayerId;
            }
        }

        // Layer 1 (highest): persisted per-tournament prefs.
        if (tournament.UscfReportPrefs is { } u)
        {
            if (!string.IsNullOrWhiteSpace(u.AffiliateId))   AffiliateId   = u.AffiliateId!;
            if (!string.IsNullOrWhiteSpace(u.ChiefTdId))     ChiefTdId     = u.ChiefTdId!;
            if (!string.IsNullOrWhiteSpace(u.AssistantTdId)) AssistantTdId = u.AssistantTdId!;
            if (!string.IsNullOrWhiteSpace(u.OtherTdNotes))  OtherTdNotes  = u.OtherTdNotes!;
            if (u.RatingSystem is char rs)         RatingSystem        = rs.ToString();
            if (u.SendCrossTable      is bool sct) SendCrossTable      = sct;
            if (u.GrandPrix           is bool gp)  GrandPrix           = gp;
            if (u.FideRated           is bool fr)  FideRated           = fr;
            if (u.IncludeSectionDates is bool isd) IncludeSectionDates = isd;
        }
    }
}
