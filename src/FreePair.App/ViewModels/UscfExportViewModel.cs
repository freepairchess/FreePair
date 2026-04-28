using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.UscfExport;

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
        FideRated:      FideRated ? 'Y' : 'N');
}
