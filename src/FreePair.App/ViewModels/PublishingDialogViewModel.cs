using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Publishing;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// Choice row in the destination picker. Bound to the
/// <c>Destination</c> ComboBox in <c>PublishingDialog.axaml</c>.
/// </summary>
public sealed record PublishingDestinationOption(
    string Key,
    string DisplayName);

/// <summary>
/// View-model for the "Publish pairing and results online" dialog.
/// Surfaces the destination picker, editable URL / event-id /
/// passcode fields (pre-filled from the active tournament), the two
/// auto-publish toggles, and a "Publish now" command that uploads
/// the current .sjson file via an <see cref="IPublishingClient"/>.
/// </summary>
/// <remarks>
/// The dialog does NOT close on a successful publish — it shows
/// inline status so the TD can see "✅ Uploaded" / "❌ 401 Invalid
/// passcode" and keep retrying / editing without losing state.
/// Close via Cancel or OK.
/// </remarks>
public sealed partial class PublishingDialogViewModel : ViewModelBase
{
    private readonly IReadOnlyDictionary<string, IPublishingClient> _clients;
    private readonly Func<string?> _getTournamentFilePath;
    private readonly Func<Tournament?> _getTournament;

    // ============== destination picker ==============

    public IReadOnlyList<PublishingDestinationOption> Destinations { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedClientDisplayName))]
    private PublishingDestinationOption? _selectedDestination;

    public string SelectedClientDisplayName =>
        SelectedDestination is null ? "" :
        _clients.TryGetValue(SelectedDestination.Key, out var c) ? c.DisplayName : "";

    // ============== editable fields ==============

    [ObservableProperty] private string? _baseUrl;

    // ============== read-only mirrors (sourced from the tournament) ==============
    // Event ID + passcode live in the .sjson Overview and are edited
    // on the Event config tab. The dialog shows them for confirmation
    // only — no duplicate editor here.

    public string? EventId   { get; }
    public string? Passcode  { get; }

    /// <summary>Masked preview of the passcode for display.</summary>
    public string  PasscodeMasked =>
        string.IsNullOrEmpty(Passcode) ? "(not set)" : new string('●', Math.Min(Passcode!.Length, 12));

    public bool HasEventId  => !string.IsNullOrWhiteSpace(EventId);
    public bool HasPasscode => !string.IsNullOrWhiteSpace(Passcode);
    public bool IsReadyToPublish => HasEventId && HasPasscode;

    // ============== auto-publish toggles (persisted via Tournament record) ==============

    [ObservableProperty] private bool _autoPublishPairings;
    [ObservableProperty] private bool _autoPublishResults;

    // ============== publish status ==============

    /// <summary>Last publish-attempt result for inline display.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    [NotifyPropertyChangedFor(nameof(StatusIsError))]
    private string? _statusMessage;

    [ObservableProperty] private bool _isPublishing;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    public bool StatusIsError    => _lastResultWasError;
    private bool _lastResultWasError;

    public PublishingDialogViewModel(
        IReadOnlyDictionary<string, IPublishingClient> clients,
        Func<Tournament?> getTournament,
        Func<string?> getTournamentFilePath,
        string baseUrlDefault,
        bool autoPublishPairingsDefault,
        bool autoPublishResultsDefault)
    {
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        _getTournament = getTournament ?? throw new ArgumentNullException(nameof(getTournament));
        _getTournamentFilePath = getTournamentFilePath ?? throw new ArgumentNullException(nameof(getTournamentFilePath));

        // Build destination options from the client dictionary. Keys
        // are stable identifiers ("nachesshub"); DisplayName comes
        // from the client itself.
        var dests = new List<PublishingDestinationOption>();
        foreach (var (key, client) in clients)
        {
            dests.Add(new PublishingDestinationOption(key, client.DisplayName));
        }
        Destinations = dests;
        SelectedDestination = Destinations.Count > 0 ? Destinations[0] : null;

        BaseUrl = baseUrlDefault;
        var t = getTournament();
        EventId  = t?.NachEventId;
        Passcode = t?.NachPasscode;
        AutoPublishPairings = autoPublishPairingsDefault;
        AutoPublishResults  = autoPublishResultsDefault;
    }

    /// <summary>
    /// Kicks off a manual publish. Disables the button while the
    /// HTTP call is in flight; writes the outcome to
    /// <see cref="StatusMessage"/> + <see cref="StatusIsError"/>.
    /// </summary>
    [RelayCommand]
    private async Task PublishNowAsync(CancellationToken ct)
    {
        if (SelectedDestination is null
            || !_clients.TryGetValue(SelectedDestination.Key, out var client))
        {
            Fail("No destination selected.");
            return;
        }

        var path = _getTournamentFilePath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Fail("No tournament file to publish. Save the tournament first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))  { Fail("Hub URL is required.");  return; }
        if (string.IsNullOrWhiteSpace(EventId))  { Fail("Event ID is required — set it on the Event tab."); return; }
        if (string.IsNullOrWhiteSpace(Passcode)) { Fail("Passcode is required — set it on the Event tab."); return; }

        try
        {
            IsPublishing = true;
            StatusMessage = "Publishing…";
            _lastResultWasError = false;

            var result = await client.PublishAsync(
                BaseUrl!, EventId!, Passcode!,
                FileType.SwissSys11SJson,
                path!,
                ct).ConfigureAwait(true);

            if (result.Success)
            {
                _lastResultWasError = false;
                StatusMessage = result.ServerFileId is null
                    ? $"✅ Uploaded to {client.DisplayName}."
                    : $"✅ Uploaded to {client.DisplayName} (file id {result.ServerFileId}).";
            }
            else
            {
                Fail($"❌ {result.ErrorMessage ?? "Unknown error."}");
            }
        }
        catch (OperationCanceledException)
        {
            Fail("Cancelled.");
        }
        finally
        {
            IsPublishing = false;
            OnPropertyChanged(nameof(HasStatusMessage));
            OnPropertyChanged(nameof(StatusIsError));
        }
    }

    private void Fail(string msg)
    {
        _lastResultWasError = true;
        StatusMessage = msg;
    }
}
