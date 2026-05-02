using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Bbp;
using FreePair.Core.Formatting;
using FreePair.Core.Publishing;
using FreePair.Core.Settings;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// View model backing the Tournament tab. Owns the loaded
/// <see cref="Core.Tournaments.Tournament"/> (if any) and manages
/// opening / closing / auto-reload via <see cref="ITournamentLoader"/>.
/// </summary>
public partial class TournamentViewModel : ViewModelBase
{
    private readonly ITournamentLoader _loader;
    private readonly ISettingsService _settingsService;
    private readonly IScoreFormatter _formatter;
    private readonly IBbpPairingEngine _pairingEngine;
    private readonly ITournamentWriter _writer;
    private readonly System.Threading.SemaphoreSlim _saveGate = new(1, 1);

    /// <summary>
    /// Publishing client used for online uploads (NA Chess Hub etc.).
    /// Injected via the secondary ctor so tests can stub it; the
    /// parameterless ctor wires up a default
    /// <see cref="NaChessHubPublishingClient"/>.
    /// </summary>
    private readonly IPublishingClient _publishingClient;

    /// <summary>
    /// Cancels any in-flight auto-publish when a newer save lands
    /// (last-write-wins coalescing).
    /// </summary>
    private CancellationTokenSource? _autoPublishCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTournament))]
    private Tournament? _tournament;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSection))]
    private SectionViewModel? _selectedSection;

    [ObservableProperty]
    private IReadOnlyList<SectionViewModel> _sections = Array.Empty<SectionViewModel>();

    /// <summary>
    /// True when the right-pane should show the event-configuration
    /// form instead of the selected section. Selecting a section from
    /// the list clears this flag (see <see cref="OnSelectedSectionChanged"/>).
    /// </summary>
    [ObservableProperty]
    private bool _isEventConfigSelected;

    /// <summary>
    /// Lazily-built view-model for the event-configuration tab. Null
    /// when no tournament is loaded.
    /// </summary>
    [ObservableProperty]
    private EventConfigViewModel? _eventConfig;

    /// <summary>
    /// When true, <see cref="OnSelectedSectionChanged"/> skips clearing
    /// <see cref="IsEventConfigSelected"/>. Set while
    /// <see cref="RebuildSections"/> re-assigns a freshly-built
    /// <see cref="SectionViewModel"/> to preserve the user's right-pane
    /// choice (e.g. Event config remains selected after an Apply).
    /// </summary>
    private bool _suppressEventConfigClearOnSelection;

    [ObservableProperty]
    private string? _currentFilePath;

    /// <summary>
    /// Lock held against the currently-open <see cref="CurrentFilePath"/>
    /// to keep two FreePair instances from auto-saving the same
    /// .sjson into each other. Released when the tournament is
    /// closed or replaced.
    /// </summary>
    private FreePair.Core.Tournaments.TournamentLock? _currentFileLock;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public TournamentViewModel()
        : this(new TournamentLoader(), new SettingsService(), new ScoreFormatter(), new BbpPairingEngine(), new SwissSysTournamentWriter(),
               new NaChessHubPublishingClient(new HttpClient()))
    {
    }

    public TournamentViewModel(
        ITournamentLoader loader,
        ISettingsService settingsService,
        IScoreFormatter formatter,
        IBbpPairingEngine pairingEngine,
        ITournamentWriter writer,
        IPublishingClient? publishingClient = null)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _pairingEngine = pairingEngine ?? throw new ArgumentNullException(nameof(pairingEngine));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _publishingClient = publishingClient ?? new NaChessHubPublishingClient(new HttpClient());
    }

    /// <summary>
    /// Callback used by <see cref="OpenCommand"/> to let the view show a
    /// native file picker. Returns the selected path, or <c>null</c> if the
    /// user cancelled.
    /// </summary>
    public Func<Task<string?>>? PickTournamentFileAsync { get; set; }

    /// <summary>
    /// Callback used by <see cref="ExportTrfCommand"/> to let the view show a
    /// native save dialog. Returns the selected path, or <c>null</c> if the
    /// user cancelled.
    /// </summary>
    public Func<string, Task<string?>>? PickExportTrfPathAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the Publish-online dialog.
    /// The delegate receives a preconfigured
    /// <see cref="PublishingDialogViewModel"/>; the view shows the
    /// dialog as modal and returns the same VM on close so the
    /// caller can read the TD's chosen auto-flags.
    /// </summary>
    public Func<PublishingDialogViewModel, Task<PublishingDialogViewModel?>>? ShowPublishingDialogAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the Manage-requested-byes
    /// dialog. Receives a preconfigured
    /// <see cref="ManageByesViewModel"/>; the view shows it as modal
    /// and returns the same VM on Save (so the caller reads
    /// <see cref="ManageByesViewModel.BuildDiffs"/>) or <c>null</c>
    /// on Cancel.
    /// </summary>
    public Func<ManageByesViewModel, Task<ManageByesViewModel?>>? ShowManageByesDialogAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the player form dialog
    /// (used for both edit and add flows). Returns the VM on Save,
    /// null on Cancel.
    /// </summary>
    public Func<PlayerFormViewModel, Task<PlayerFormViewModel?>>? ShowPlayerFormDialogAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the section form dialog
    /// (used for the add-section flow). Returns the VM on Save,
    /// null on Cancel.
    /// </summary>
    public Func<SectionFormViewModel, Task<SectionFormViewModel?>>? ShowSectionFormDialogAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the New-event dialog.
    /// Returns the VM on Create, null on Cancel.
    /// </summary>
    public Func<NewEventViewModel, Task<NewEventViewModel?>>? ShowNewEventDialogAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens a save-file picker with
    /// <c>.sjson</c> file type and returns the chosen path (or null
    /// on cancel). Used by the New-event flow.
    /// </summary>
    public Func<string /*suggestedFolder*/, string /*suggestedName*/, Task<string?>>? PickNewEventSavePathAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens an open-file picker for
    /// CSV / TSV / XLSX roster files. Returns the chosen local path
    /// or null on cancel.
    /// </summary>
    public Func<Task<string?>>? PickPlayerImportFileAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the "Open from online
    /// registry (by event ID)" dialog. Returns the VM on OK, null
    /// on Cancel.
    /// </summary>
    public Func<OpenFromRegistryViewModel, Task<OpenFromRegistryViewModel?>>? ShowOpenFromRegistryDialogAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the "Browse online events"
    /// dialog. Returns the VM on OK (selected event + passcode),
    /// null on Cancel.
    /// </summary>
    public Func<BrowseRegistryEventsViewModel, Task<BrowseRegistryEventsViewModel?>>? ShowBrowseRegistryEventsDialogAsync { get; set; }

    /// <summary>
    /// View-supplied callback that opens the "Export USCF report
    /// files" dialog. Returns the VM on OK (caller maps to
    /// UscfExportOptions and runs the exporter), null on Cancel.
    /// </summary>
    public Func<UscfExportViewModel, Task<UscfExportViewModel?>>? ShowUscfExportDialogAsync { get; set; }

    // ============ Online publishing (session-only, per-tournament) ============

    /// <summary>Sticky URL used by the Publish dialog. Seeded from <see cref="AppSettings.NaChessHubBaseUrl"/>.</summary>
    [ObservableProperty] private string _publishBaseUrl = "https://nachesshub.com";

    /// <summary>When true, auto-publish the current <c>.sjson</c> after a pair-next-round save.</summary>
    [ObservableProperty] private bool _autoPublishPairings;

    /// <summary>When true, auto-publish the current <c>.sjson</c> after a result is entered.</summary>
    [ObservableProperty] private bool _autoPublishResults;

    /// <summary>
    /// Browser URL for the most recent successful publish (points at
    /// <c>{hubBaseUrl}/EventFiles?EventID={nachEventId}</c>). Reset to
    /// <see langword="null"/> on every publish attempt; set on success
    /// so the toolbar's "Published to …" message can be rendered as a
    /// clickable link. Consumed by <c>TournamentView</c>'s toolbar.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastPublishedUrl))]
    private string? _lastPublishedUrl;

    public bool HasLastPublishedUrl => !string.IsNullOrEmpty(LastPublishedUrl);

    /// <summary>
    /// Wall-clock timestamp of the most recent successful publish.
    /// Paired with <see cref="LastPublishedUrl"/> — both are set on
    /// success and cleared to <see langword="null"/> at the start of
    /// every new publish attempt, so a failed retry doesn't display
    /// a stale "last published at…" label in the toolbar.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastPublishedAtDisplay))]
    private DateTimeOffset? _lastPublishedAt;

    /// <summary>
    /// Short local-time rendering of <see cref="LastPublishedAt"/>
    /// for the toolbar label (e.g. <c>"2026-04-23 15:42:03"</c>).
    /// Empty when the timestamp is null so the binding renders
    /// nothing.
    /// </summary>
    public string LastPublishedAtDisplay =>
        LastPublishedAt is { } ts ? ts.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "";

    /// <summary>
    /// Wall-clock timestamp of the most recent successful save of
    /// the current tournament (auto-save after a mutation, or the
    /// writer-backed save that stamps the publish timestamp). Seeded
    /// from the file's on-disk last-write time when a tournament is
    /// opened, so the toolbar "last saved at …" label appears
    /// immediately without requiring a fresh save.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastSavedAtDisplay))]
    [NotifyPropertyChangedFor(nameof(HasLastSavedAt))]
    private DateTimeOffset? _lastSavedAt;

    public bool HasLastSavedAt => LastSavedAt is not null;

    public string LastSavedAtDisplay =>
        LastSavedAt is { } ts ? ts.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "";

    /// <summary>
    /// Flag set by <see cref="OnSectionResultChanged"/> /
    /// <see cref="OnSectionPairNextRoundAsync"/> immediately before the
    /// triggered auto-save so the subsequent
    /// <see cref="PersistCurrentTournamentAsync"/> call knows whether to
    /// consult <see cref="AutoPublishPairings"/> /
    /// <see cref="AutoPublishResults"/>. Cleared after each publish
    /// attempt.
    /// </summary>
    private bool _autoPublishPairingsPending;
    private bool _autoPublishResultsPending;

    /// <summary>
    /// Callback used when pairing round 1 to let the view show a dialog
    /// asking the TD which colour the top seed should receive on board 1.
    /// Returns <c>null</c> if the user cancelled.
    /// </summary>
    public Func<Task<InitialColor?>>? PromptInitialColorAsync { get; set; }

    /// <summary>
    /// Callback opened before every "Pair next round" action so the
    /// TD can confirm or override the section's starting board
    /// number for this specific round. Args: section name, upcoming
    /// round number, current <c>Section.FirstBoard</c> (or 1 if
    /// null), recommended board from
    /// <see cref="BoardNumberRecommender"/>. Returns the chosen
    /// value, or <c>null</c> when the TD cancels (pairing aborts).
    /// </summary>
    public Func<string, int, int, int, Task<int?>>? PromptStartingBoardAsync { get; set; }

    /// <summary>
    /// Callback opened by the 🔢 Renumber boards toolbar button —
    /// shows an interactive review dialog where the TD can edit
    /// each section's starting board, see overlap warnings, and
    /// confirm. Returns the populated VM on Apply (caller
    /// extracts the chosen values via
    /// <see cref="RenumberBoardsViewModel.SnapshotChosenStartingBoards"/>),
    /// or <c>null</c> on Cancel.
    /// </summary>
    public Func<RenumberBoardsViewModel, Task<RenumberBoardsViewModel?>>? ShowRenumberBoardsDialogAsync { get; set; }

    /// <summary>
    /// Callback opened by the "Pair all sections" toolbar button —
    /// shows a status dashboard for every section + a confirm
    /// button that runs the standard pairing flow on every Ready
    /// section. Returns the VM on Apply, null on Cancel.
    /// </summary>
    public Func<PairAllSectionsViewModel, Task<PairAllSectionsViewModel?>>? ShowPairAllSectionsDialogAsync { get; set; }

    /// <summary>
    /// Callback used by destructive commands (e.g. Delete round) to prompt
    /// the user for confirmation. Returns <c>true</c> when the user confirms.
    /// Parameters: title, message, confirm-button label.
    /// </summary>
    public Func<string, string, string, Task<bool>>? PromptConfirmAsync { get; set; }

    /// <summary>
    /// Callback opened immediately after a new round has been appended,
    /// letting the TD inspect the proposed pairings and apply
    /// intervention mutations (colour swap, board swap, late ½-pt bye)
    /// before the round is persisted. Returns the (possibly mutated)
    /// tournament on commit, or <c>null</c> when the TD cancels — the
    /// host then reverts via
    /// <see cref="TournamentMutations.DeleteLastRound"/>.
    /// Parameters: working tournament, section name, new round number,
    /// unresolved-conflict warnings from the swapper.
    /// </summary>
    public Func<Tournament, string, int, IReadOnlyList<string>, Task<Tournament?>>? PromptPairingPreviewAsync { get; set; }

    /// <summary>
    /// Transient status message shown while an auto-save is in progress.
    /// Cleared on success.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSaving))]
    private string? _saveStatus;

    public bool IsSaving => !string.IsNullOrEmpty(SaveStatus);

    public bool HasTournament => Tournament is not null;

    public bool HasSelectedSection => SelectedSection is not null;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnTournamentChanged(Tournament? value)
    {
        RebuildSections();

        // Rebuild the event-config VM to track the new tournament's
        // identity (so its Reset pulls fresh values). Discarded when
        // no tournament is loaded.
        if (value is null)
        {
            EventConfig = null;
            IsEventConfigSelected = false;
        }
        else if (EventConfig is null)
        {
            EventConfig = new EventConfigViewModel(
                getTournament: () => Tournament!,
                setTournament: t => Tournament = t);
        }
        else
        {
            EventConfig.Reset();
        }
    }

    partial void OnSelectedSectionChanged(SectionViewModel? value)
    {
        // Picking a real section returns focus to the section pane —
        // unless we're in the middle of a programmatic rebuild (e.g.
        // after Apply from the event-config form), in which case the
        // user's right-pane choice should be preserved.
        if (value is not null && !_suppressEventConfigClearOnSelection)
        {
            IsEventConfigSelected = false;
        }
    }

    /// <summary>
    /// Switches the right pane to the event-configuration form.
    /// No-op when no tournament is loaded.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ShowEventConfig()
    {
        if (Tournament is null) return;
        EventConfig?.Reset();
        IsEventConfigSelected = true;
    }

    /// <summary>
    /// Rebuilds <see cref="Sections"/> from the current tournament, preserving
    /// the previously selected section and round when possible.
    /// </summary>
    public void RebuildSections()
    {
        var previouslySelectedSection = SelectedSection?.Name;
        var previouslySelectedRound = SelectedSection?.SelectedRound?.Number;
        var previouslySelectedTab = SelectedSection?.SelectedTabIndex ?? 0;

        // Suppress the "selecting a section clears the event-config flag"
        // side-effect while we re-point SelectedSection at a freshly-built
        // VM — the user didn't change their pane choice.
        _suppressEventConfigClearOnSelection = true;
        try
        {
            DetachSectionEvents();

            if (Tournament is null)
            {
                Sections = Array.Empty<SectionViewModel>();
                SelectedSection = null;
                return;
            }

            var newSections = Tournament.Sections
                .Select(s => new SectionViewModel(s, _formatter))
                .ToArray();

            foreach (var vm in newSections)
            {
                AttachSectionEvents(vm);
            }

            Sections = newSections;

            var newSelected = previouslySelectedSection is null
                ? newSections.FirstOrDefault()
                : newSections.FirstOrDefault(s => s.Name == previouslySelectedSection)
                  ?? newSections.FirstOrDefault();

            if (newSelected is not null)
            {
                if (previouslySelectedRound is int targetRound)
                {
                    var matching = newSelected.AvailableRounds.FirstOrDefault(r => r.Number == targetRound);
                    if (matching is not null)
                    {
                        newSelected.SelectedRound = matching;
                    }
                }

                newSelected.SelectedTabIndex = previouslySelectedTab;
            }

            SelectedSection = newSelected;
        }
        finally
        {
            _suppressEventConfigClearOnSelection = false;
        }
    }

    /// <summary>
    /// Loads persisted state and, if a previously-opened tournament file
    /// still exists, reopens it automatically.
    /// </summary>
    /// <param name="skipAutoLoadLast">
    /// When <c>true</c>, the LastTournamentFilePath auto-reopen is
    /// skipped — typically because a CLI arg supplied an explicit
    /// path the caller will load right after Initialize. Without
    /// this gate, an instance launched via the multi-instance
    /// handoff would briefly try to load the previous instance's
    /// tournament (the last value persisted to settings) and bounce
    /// off its lock with a confusing "already open" banner.
    /// </param>
    public async Task InitializeAsync(bool skipAutoLoadLast = false)
    {
        try
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            _formatter.UseAsciiOnly = settings.UseAsciiOnly;

            if (!skipAutoLoadLast)
            {
                var path = settings.LastTournamentFilePath;
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    await LoadAsync(path).ConfigureAwait(true);
                }
            }
        }
        catch (Exception ex)
        {
            // Never block startup on settings/auto-load failures.
            ErrorMessage = $"Failed to auto-load last tournament: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (PickTournamentFileAsync is null)
        {
            return;
        }

        var picked = await PickTournamentFileAsync().ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(picked))
        {
            if (TryRouteOrShortCircuit(picked!)) return;
            await LoadAsync(picked!).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Creates a brand-new tournament from scratch. Prompts the TD
    /// for a title (and optionally a first section), then opens a
    /// save-file picker and writes an initial <c>.sjson</c>. After
    /// return, the new tournament is loaded as
    /// <see cref="Tournament"/> with <see cref="CurrentFilePath"/>
    /// set to the picked path, so subsequent edits auto-save to
    /// that file via <see cref="PersistCurrentTournamentAsync"/>.
    /// </summary>
    [RelayCommand]
    private async Task NewAsync()
    {
        if (ShowNewEventDialogAsync is null || PickNewEventSavePathAsync is null) return;

        var dialogVm = new NewEventViewModel();
        var result = await ShowNewEventDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return; // cancelled

        if (!result.TryValidate(out var firstRounds))
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        // Suggest a filename + target folder based on the title and
        // the TD's configured TournamentsRootFolder. Default layout:
        // {root}/{sanitized title}/{sanitized title}.sjson — one
        // folder per event with the .sjson + any exported PDFs all
        // living together. The per-event folder is mkdir'd so the
        // native save dialog opens right inside it.
        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        var root = FreePair.Core.Tournaments.TournamentFolder.ResolveRoot(settings);
        var safeTitle = FreePair.Core.Tournaments.TournamentFolder.SanitizeForFileName(result.EventTitle);
        var eventFolder = FreePair.Core.Tournaments.TournamentFolder.EnsureEventFolder(root, result.EventTitle);
        var suggested = $"{safeTitle}.sjson";
        var path = await PickNewEventSavePathAsync(eventFolder, suggested).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(path)) return;

        // Build the tournament. Only the title is seeded; every
        // other Overview field stays null and the TD fills in dates
        // / location / etc. via the event-config dialog afterward.
        // Five fields are positional without defaults; the rest
        // default to null on the Tournament record.
        var tournament = new Tournament(
            Title: result.EventTitle.Trim(),
            StartDate: null,
            EndDate: null,
            TimeControl: null,
            NachEventId: null,
            Sections: System.Array.Empty<Section>());

        if (!string.IsNullOrWhiteSpace(result.FirstSectionName))
        {
            tournament = TournamentMutations.AddSection(
                tournament,
                name: result.FirstSectionName.Trim(),
                kind: result.FirstSectionKind.Kind,
                finalRound: firstRounds,
                timeControl: result.FirstSectionTimeControl);
        }

        // Seed state + persist via the standard save path so the new
        // file is created on disk through the writer's missing-file
        // scaffold branch.
        // Multi-instance routing: when this instance already has a
        // tournament open, write the new event directly to disk via
        // the writer (so the .sjson exists) and hand it off to a
        // fresh process instead of replacing the in-memory model.
        // We pre-check the path against the same rules as
        // TryRouteOrShortCircuit so the picker's "Save As" doesn't
        // silently clobber a file that's already open here or in
        // another instance.
        if (IsSameFileAlreadyOpen(path))
        {
            ErrorMessage =
                $"'{System.IO.Path.GetFileName(path)}' is already open in this window.";
            return;
        }
        if (FreePair.Core.Tournaments.TournamentLock.IsHeldByAnotherProcess(path))
        {
            ErrorMessage =
                $"'{System.IO.Path.GetFileName(path)}' is already open in another FreePair window. " +
                "Switch to that window, or close it first.";
            return;
        }

        if (HasOpenTournament)
        {
            try
            {
                await _writer.SaveAsync(path, tournament).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to create new tournament file: {ex.Message}";
                return;
            }
            if (TryHandoffToNewInstance(path))
            {
                return;
            }
            // exe-path lookup failed — fall through to the in-place
            // load below as a last resort.
        }

        Tournament = tournament;
        CurrentFilePath = path;
        LastSavedAt = null;
        LastPublishedAt = null;
        LastPublishedUrl = null;
        ErrorMessage = null;
        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Shared HTTP client used by registry calls. A single instance
    /// per app gets connection pooling + HTTP/2 reuse. Disposal is
    /// left to the GC — the lifetime is effectively process-wide.
    /// </summary>
    private static readonly System.Net.Http.HttpClient s_registryHttp =
        new() { Timeout = System.TimeSpan.FromSeconds(30) };

    /// <summary>
    /// "Open from online registry (by event ID + passcode)" flow.
    /// Opens the dialog, calls the chosen registry, saves the
    /// returned bytes to the standard per-event folder under
    /// TournamentsRootFolder, then loads it.
    /// </summary>
    [RelayCommand]
    private async Task OpenFromRegistryAsync()
    {
        if (ShowOpenFromRegistryDialogAsync is null) return;

        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        var registries = FreePair.Core.Registries.RegistryCatalog.Build(s_registryHttp, settings);
        if (registries.Count == 0)
        {
            ErrorMessage = "No online registries are configured.";
            return;
        }

        var dialogVm = new OpenFromRegistryViewModel(registries);
        var result = await ShowOpenFromRegistryDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return;

        await DownloadAndOpenAsync(result.SelectedRegistry, result.EventId, result.Passcode, suggestedName: null, settings)
            .ConfigureAwait(true);
    }

    /// <summary>
    /// "Browse online events" flow. Opens the list dialog, and on
    /// confirm runs the same download path as
    /// <see cref="OpenFromRegistryAsync"/> using the picked event's
    /// name for the folder / filename.
    /// </summary>
    [RelayCommand]
    private async Task BrowseRegistryEventsAsync()
    {
        if (ShowBrowseRegistryEventsDialogAsync is null) return;

        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        var registries = FreePair.Core.Registries.RegistryCatalog.Build(s_registryHttp, settings);
        var listable = registries.Where(r => r.SupportsListEvents).ToArray();
        if (listable.Length == 0)
        {
            ErrorMessage = "No online registries currently support browsing events.";
            return;
        }

        var dialogVm = new BrowseRegistryEventsViewModel(listable);
        var result = await ShowBrowseRegistryEventsDialogAsync(dialogVm).ConfigureAwait(true);
        if (result?.ChosenEvent is null) return;

        // Hand off to the by-id Open flow, pre-filling the chosen
        // event's id and locking the registry to the one the TD just
        // browsed. The by-id dialog collects the passcode (we don't
        // ask for it on the browse list itself) and then runs the
        // standard DownloadAndOpenAsync pipeline. Carrying the
        // suggested name through means the per-event folder uses the
        // browse-list name verbatim, not the title sniffed from the
        // payload.
        if (ShowOpenFromRegistryDialogAsync is null) return;

        var byIdVm = new OpenFromRegistryViewModel(new[] { result.SelectedRegistry })
        {
            EventId = result.ChosenEvent.Id,
            PrefilledEvent = result.ChosenEvent,
        };
        var confirmed = await ShowOpenFromRegistryDialogAsync(byIdVm).ConfigureAwait(true);
        if (confirmed is null) return;

        await DownloadAndOpenAsync(
            confirmed.SelectedRegistry,
            confirmed.EventId,
            confirmed.Passcode,
            suggestedName: result.ChosenEvent.Name,
            settings).ConfigureAwait(true);
    }

    /// <summary>
    /// "Renumber section starting boards" flow. Auto-applies the
    /// <see cref="BoardNumberRecommender"/>'s suggested
    /// <c>FirstBoard</c> to every section so multi-section events
    /// don't reuse the same physical board #1 across Open / U1700
    /// / etc. Future-only — already-paired rounds stay at their
    /// existing board numbers (a deliberate choice so printed
    /// pairings don't drift after the TD has handed them out).
    /// </summary>
    /// <summary>
    /// "Pair all sections" toolbar flow. Opens a status-dashboard
    /// dialog showing every section's readiness (and start board)
    /// so the TD can confirm before kicking off a batch pair.
    /// Each ready section then runs through the same pairing flow
    /// the per-section "Pair next round" button uses (BBP →
    /// preview → persist), but with the per-pairing
    /// starting-board prompt skipped — start boards have already
    /// been reviewed in the dialog. Errors on individual sections
    /// are collected and reported but don't abort the rest of the
    /// batch.
    /// </summary>
    [RelayCommand]
    private async Task PairAllSectionsAsync()
    {
        if (Tournament is null) return;
        if (ShowPairAllSectionsDialogAsync is null) return;

        var dialogVm = new PairAllSectionsViewModel(Tournament);
        if (dialogVm.Rows.Count == 0) return;

        var result = await ShowPairAllSectionsDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return; // cancelled

        // Apply any per-section StartingBoard edits the TD made in
        // the dashboard before kicking off the batch pair, so each
        // section's first round lands at the chosen offset. Skipped
        // when the value is unchanged (no-op mutation).
        var chosenBoards = result.SnapshotChosenStartingBoards();
        var t = Tournament;
        foreach (var (name, value) in chosenBoards)
        {
            var section = t.Sections.FirstOrDefault(s => s.Name == name);
            if (section is null) continue;
            var newValue = value <= 1 ? (int?)null : value;
            if (section.FirstBoard == newValue) continue;
            t = TournamentMutations.SetSectionFirstBoard(t, name, newValue);
        }

        // Apply any per-section pairing-engine overrides the TD
        // picked in the dashboard. Only R1-bound sections (rows
        // where IsEngineEditable was true) are present in the
        // dict — sections past round 1 are locked. The mutation
        // throws if a row slipped through, so we wrap in try and
        // skip locked sections silently.
        var chosenEngines = result.SnapshotChosenPairingEngines();
        foreach (var (name, engine) in chosenEngines)
        {
            var section = t.Sections.FirstOrDefault(s => s.Name == name);
            if (section is null) continue;
            if (section.PairingEngine == engine) continue;
            try
            {
                t = TournamentMutations.SetSectionPairingEngine(t, name, engine);
            }
            catch (System.InvalidOperationException) { /* round-paired lock */ }
        }
        if (!ReferenceEquals(t, Tournament))
        {
            Tournament = t;
        }

        var readyNames = result.ReadySectionNames;
        if (readyNames.Count == 0)
        {
            SaveStatus = "No sections were ready to pair.";
            return;
        }

        var paired = new System.Collections.Generic.List<string>();
        var failed = new System.Collections.Generic.List<string>();

        foreach (var name in readyNames)
        {
            // Re-snapshot Sections each iteration — every successful
            // pair mutates Tournament, and we want to re-check the
            // section's state in case (e.g.) a player just got
            // withdrawn between rows.
            var sectionVm = Sections.FirstOrDefault(s => s.Name == name);
            if (sectionVm is null) { failed.Add($"{name} (not found)"); continue; }

            var beforeRoundCount = sectionVm.Section.Rounds.Count;
            try
            {
                await PairSectionNextRoundAsync(sectionVm, skipStartingBoardPrompt: true).ConfigureAwait(true);
            }
            catch (System.Exception ex)
            {
                failed.Add($"{name} ({ex.Message})");
                continue;
            }

            // The pair-next flow exits silently on user cancel of
            // the preview dialog (no new round committed). We
            // detect that by comparing round count before/after.
            var afterSection = Tournament?.Sections.FirstOrDefault(s => s.Name == name);
            var afterRoundCount = afterSection?.Rounds.Count ?? beforeRoundCount;
            if (afterRoundCount > beforeRoundCount) paired.Add(name);
        }

        SaveStatus = (paired.Count, failed.Count) switch
        {
            (0, 0) => "No sections paired (all cancelled by you).",
            (_, 0) => $"Paired {paired.Count} section(s): {string.Join(", ", paired)}.",
            (0, _) => $"Failed to pair: {string.Join("; ", failed)}.",
            _      => $"Paired {paired.Count} section(s): {string.Join(", ", paired)}. " +
                      $"Failed: {string.Join("; ", failed)}.",
        };
    }

    [RelayCommand]
    private async Task RenumberSectionBoardsAsync()
    {
        if (Tournament is null) return;
        if (ShowRenumberBoardsDialogAsync is null)
        {
            // Headless / smoke-test fallback: behave like the legacy
            // one-click button so we don't deadlock waiting for a
            // dialog that'll never appear.
            await ApplyRecommendedSilentAsync().ConfigureAwait(true);
            return;
        }

        var dialogVm = new RenumberBoardsViewModel(Tournament);
        if (dialogVm.Rows.Count == 0) return;

        var result = await ShowRenumberBoardsDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return; // cancelled

        var chosen = result.SnapshotChosenStartingBoards();
        var t = Tournament;
        var changedNames = new System.Collections.Generic.List<string>();
        foreach (var (name, value) in chosen)
        {
            var section = t.Sections.FirstOrDefault(s => s.Name == name);
            if (section is null) continue;
            var newValue = value <= 1 ? (int?)null : value;
            if (section.FirstBoard == newValue) continue;
            t = TournamentMutations.SetSectionFirstBoard(t, name, newValue);
            changedNames.Add($"{name} → {value}");
        }

        if (changedNames.Count == 0)
        {
            SaveStatus = "No changes — section starting boards already match those values.";
            return;
        }

        Tournament = t;
        try { await PersistCurrentTournamentAsync().ConfigureAwait(true); } catch { /* best-effort */ }
        SaveStatus = $"Renumbered {changedNames.Count} section(s): {string.Join(", ", changedNames)}.";
    }

    /// <summary>
    /// Headless fallback for the renumber flow when no dialog
    /// callback is wired (designer / unit tests). Applies the
    /// recommender's values directly.
    /// </summary>
    private async Task ApplyRecommendedSilentAsync()
    {
        if (Tournament is null) return;
        var recommended = BoardNumberRecommender.Recommend(Tournament);
        var t = Tournament;
        var changed = false;
        foreach (var section in t.Sections)
        {
            if (!recommended.TryGetValue(section.Name, out var rec)) continue;
            if (section.FirstBoard == rec) continue;
            t = TournamentMutations.SetSectionFirstBoard(t, section.Name, rec);
            changed = true;
        }
        if (changed)
        {
            Tournament = t;
            try { await PersistCurrentTournamentAsync().ConfigureAwait(true); } catch { }
        }
    }

    /// <summary>
    /// "Export USCF report files" flow. Opens the metadata dialog
    /// (pre-filled from settings), runs <c>UscfExporter</c> with
    /// the supplied options, drops the three DBFs into the current
    /// tournament folder using the .sjson basename as the file
    /// prefix so multiple events can coexist, and reveals the
    /// folder in Explorer.
    /// </summary>
    [RelayCommand]
    private async Task ExportUscfAsync()
    {
        if (Tournament is null || string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            ErrorMessage = "Open a tournament before exporting USCF reports.";
            return;
        }
        if (ShowUscfExportDialogAsync is null) return;

        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        var dialogVm = new UscfExportViewModel();
        // Priority cascade: per-tournament prefs > Overview-derived
        // (Organizer ID / Event city/state/zip/country / Delegations
        // first TD as chief, second as assistant) > AppSettings.
        dialogVm.Prefill(Tournament!, settings);

        var confirmed = await ShowUscfExportDialogAsync(dialogVm).ConfigureAwait(true);
        if (confirmed is null) return;

        // Persist whatever the TD typed back as both the new
        // app-wide defaults AND the per-tournament sticky prefs so
        // subsequent exports of the SAME event pre-fill instantly
        // and a NEW event still pre-fills with the latest seen
        // values.
        settings.UscfChiefTdId     = confirmed.ChiefTdId.Trim();
        settings.UscfAssistantTdId = confirmed.AssistantTdId.Trim();
        settings.UscfAffiliateId   = confirmed.AffiliateId.Trim();
        settings.UscfCity          = confirmed.City.Trim();
        settings.UscfState         = confirmed.State.Trim().ToUpperInvariant();
        settings.UscfZipCode       = confirmed.ZipCode.Trim();
        settings.UscfCountry       = string.IsNullOrWhiteSpace(confirmed.Country) ? "USA" : confirmed.Country.Trim();
        try { await _settingsService.SaveAsync(settings).ConfigureAwait(true); } catch { /* defaults are best-effort */ }

        // Per-tournament sticky prefs: stamp them onto the
        // Tournament record and persist so the writer round-trips
        // them as "FreePair USCF *" Overview keys.
        Tournament = TournamentMutations.SetUscfReportPrefs(Tournament!, confirmed.ToPersistedPrefs());
        try { await PersistCurrentTournamentAsync().ConfigureAwait(true); } catch { /* best-effort */ }

        var folder = System.IO.Path.GetDirectoryName(CurrentFilePath)
                     ?? System.IO.Directory.GetCurrentDirectory();
        var prefix = System.IO.Path.GetFileNameWithoutExtension(CurrentFilePath) + "_";

        try
        {
            var exporter = new FreePair.Core.UscfExport.UscfExporter();
            var written = exporter.Export(Tournament!, confirmed.ToOptions(), folder, prefix);
            SaveStatus = $"USCF report files written: {written.Count} files in '{folder}'.";

            // Reveal the folder so the TD can drag-and-drop the
            // three DBFs onto the USCF rater. Same pattern as the
            // post-PDF Explorer reveal.
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{written[0]}\"",
                        UseShellExecute = false,
                    });
                }
            }
            catch { /* best-effort */ }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Failed to write USCF report files: {ex.Message}";
        }
    }

    /// <summary>
    /// Shared download + save + load pipeline used by both registry
    /// entry points. <paramref name="suggestedName"/> is the event
    /// name from the browse list when known; for the by-id flow it
    /// comes from the downloaded .sjson's Overview.Title after the
    /// first parse.
    /// </summary>
    private async Task DownloadAndOpenAsync(
        FreePair.Core.Registries.IExternalRegistry registry,
        string eventId,
        string passcode,
        string? suggestedName,
        FreePair.Core.Settings.AppSettings settings)
    {
        ErrorMessage = null;
        SaveStatus = $"Downloading from {registry.DisplayName}…";
        byte[] bytes;
        try
        {
            bytes = await registry.DownloadSjsonAsync(eventId, passcode).ConfigureAwait(true);
        }
        catch (System.Exception ex)
        {
            SaveStatus = null;
            ErrorMessage = ex.Message;
            return;
        }
        SaveStatus = null;

        // If no name was pre-supplied (by-id flow), sniff the title
        // from the downloaded payload's Overview.Title — falls back
        // to the event ID when the JSON doesn't include one.
        var name = suggestedName;
        if (string.IsNullOrWhiteSpace(name))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(bytes);
                if (doc.RootElement.TryGetProperty("Overview", out var ov) &&
                    ov.TryGetProperty("Tournament title", out var t) &&
                    t.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    name = t.GetString();
                }
            }
            catch
            {
                // Malformed JSON is handled by the load step below.
            }
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Event {eventId}";
        }

        var root = FreePair.Core.Tournaments.TournamentFolder.ResolveRoot(settings);
        var folder = FreePair.Core.Tournaments.TournamentFolder.EnsureEventFolder(root, name);
        var path = FreePair.Core.Tournaments.TournamentFolder.ResolveUniqueFilePath(folder, name, ".sjson");

        try
        {
            await System.IO.File.WriteAllBytesAsync(path, bytes).ConfigureAwait(true);
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Failed to save downloaded tournament: {ex.Message}";
            return;
        }

        // Same routing as OpenAsync — same-file short-circuit,
        // cross-instance lock detection, otherwise spawn-or-load.
        if (TryRouteOrShortCircuit(path)) return;
        await LoadAsync(path).ConfigureAwait(true);
    }

    [RelayCommand]
    private void Close()
    {
        Tournament = null;
        CurrentFilePath = null;
        ErrorMessage = null;
        LastSavedAt = null;
        LastPublishedAt = null;
        LastPublishedUrl = null;
        _currentFileLock?.Dispose();
        _currentFileLock = null;
    }

    /// <summary>
    /// CLI entry point: called once at app startup with the path
    /// passed via <c>FreePair.App.exe &lt;file&gt;</c>. Identical to
    /// the in-app Open flow except it never tries to spawn a new
    /// instance (we ARE the new instance).
    /// </summary>
    public Task LoadFromStartupArgsAsync(string filePath) =>
        LoadAsync(filePath);

    /// <summary>
    /// True when <paramref name="filePath"/> is the same file this
    /// window already has loaded (case-insensitive on Windows,
    /// case-sensitive elsewhere — same rule as the lock). Used to
    /// short-circuit "Open the same tournament again" with a no-op
    /// instead of spawning another instance.
    /// </summary>
    private bool IsSameFileAlreadyOpen(string filePath)
    {
        if (string.IsNullOrWhiteSpace(CurrentFilePath)) return false;
        try
        {
            var a = System.IO.Path.GetFullPath(CurrentFilePath);
            var b = System.IO.Path.GetFullPath(filePath);
            var cmp = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(a, b, cmp);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Decides what to do when the TD picks <paramref name="filePath"/>
    /// to open / download into. Returns <c>true</c> when the caller
    /// should bail out (already handled — either a no-op for the
    /// same file or a successful handoff to a new instance);
    /// <c>false</c> when the caller should fall through and load
    /// in place. Sets <see cref="ErrorMessage"/> on conflict cases.
    /// </summary>
    private bool TryRouteOrShortCircuit(string filePath)
    {
        // 1. Same file already open in THIS window — explicit no-op.
        if (IsSameFileAlreadyOpen(filePath))
        {
            ErrorMessage =
                $"'{System.IO.Path.GetFileName(filePath)}' is already open in this window.";
            return true;
        }

        // 2. Same file open in ANOTHER FreePair instance — refuse
        //    here instead of spawning a new (empty) window that just
        //    shows the same error.
        if (FreePair.Core.Tournaments.TournamentLock.IsHeldByAnotherProcess(filePath))
        {
            ErrorMessage =
                $"'{System.IO.Path.GetFileName(filePath)}' is already open in another FreePair window. " +
                "Switch to that window, or close it first.";
            return true;
        }

        // 3. This window has its own tournament loaded — hand off
        //    to a fresh process so both events stay editable.
        if (HasOpenTournament && TryHandoffToNewInstance(filePath))
        {
            return true;
        }

        // 4. Empty window or handoff failed — fall through to
        //    in-place load.
        return false;
    }

    /// <summary>
    /// True when the current instance already has a tournament open
    /// (or one is mid-load). The Open / OpenFromRegistry / etc.
    /// commands consult this and route to a fresh process when set
    /// so the TD can pair multiple events side-by-side.
    /// </summary>
    private bool HasOpenTournament =>
        Tournament is not null || !string.IsNullOrWhiteSpace(CurrentFilePath) || IsLoading;

    /// <summary>
    /// Spawns a second FreePair process pointed at
    /// <paramref name="filePath"/>. Returns <c>true</c> when the
    /// process started (caller should bail out of the in-place
    /// load). Returns <c>false</c> when we couldn't determine our
    /// own executable path or the launch failed — caller falls
    /// back to loading in place.
    /// </summary>
    private bool TryHandoffToNewInstance(string filePath)
    {
        var exe = System.Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe)) return false;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = false,
                WorkingDirectory = System.IO.Path.GetDirectoryName(exe) ?? string.Empty,
            };
            psi.ArgumentList.Add(filePath);
            var proc = System.Diagnostics.Process.Start(psi);
            return proc is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Opens the section form dialog in Add mode, then dispatches
    /// through <see cref="TournamentMutations.AddSection"/> and
    /// persists. Bound to the "➕ Add section" button on
    /// <c>TournamentView</c>.
    /// </summary>
    [RelayCommand]
    private async Task AddSectionAsync()
    {
        if (Tournament is null || ShowSectionFormDialogAsync is null) return;

        var dialogVm = SectionFormViewModel.ForAdd(Tournament.Title ?? "(untitled)");
        var result = await ShowSectionFormDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return; // cancelled

        if (!result.TryValidate(out var finalRound))
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        try
        {
            Tournament = TournamentMutations.AddSection(
                Tournament,
                name: result.Name,
                kind: result.Kind.Kind,
                finalRound: finalRound,
                timeControl: result.TimeControl,
                title: result.SectionTitle);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add section: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);

        // Jump to the newly-created section so the TD can start
        // populating it immediately.
        SelectedSection = Sections.FirstOrDefault(s => s.Name == result.Name.Trim());
    }

    [RelayCommand]
    private async Task ExportTrfAsync()
    {
        if (Tournament is null || SelectedSection is null || PickExportTrfPathAsync is null)
        {
            return;
        }

        var defaultName = $"{SelectedSection.Name}.trf";
        var target = await PickExportTrfPathAsync(defaultName).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        try
        {
            await using var writer = new StreamWriter(target, append: false, System.Text.Encoding.ASCII);
            Core.Trf.TrfWriter.Write(Tournament, SelectedSection.Section, writer);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to export TRF: {ex.Message}";
        }
    }

    private void AttachSectionEvents(SectionViewModel vm)
    {
        vm.ParentTournamentVm = this;
        vm.ResultChanged += OnSectionResultChanged;
        vm.PairNextRoundRequested += OnSectionPairNextRoundAsync;
        vm.DeleteLastRoundRequested += OnSectionDeleteLastRoundAsync;
        vm.SoftDeleteRequested += OnSectionSoftDeleteAsync;
        vm.UndeleteRequested   += OnSectionUndeleteAsync;
        vm.HardDeleteRequested += OnSectionHardDeleteAsync;
        vm.PlayerSoftDeleteRequested += OnPlayerSoftDeleteAsync;
        vm.PlayerUndeleteRequested   += OnPlayerUndeleteAsync;
        vm.PlayerHardDeleteRequested += OnPlayerHardDeleteAsync;
        vm.PlayerWithdrawRequested   += OnPlayerWithdrawAsync;
        vm.PlayerUnwithdrawRequested += OnPlayerUnwithdrawAsync;
        vm.PlayerManageByesRequested += OnPlayerManageByesAsync;
        vm.PlayerEditRequested       += OnPlayerEditAsync;
        vm.PlayerAddRequested        += OnPlayerAddAsync;
        vm.PlayerImportRequested     += OnPlayerImportAsync;
        vm.MoveRequested             += OnSectionMoveAsync;
        vm.PairingEngineChangeRequested += OnSectionPairingEngineChangeAsync;
    }

    private void DetachSectionEvents()
    {
        foreach (var vm in Sections)
        {
            vm.ParentTournamentVm = null;
            vm.ResultChanged -= OnSectionResultChanged;
            vm.PairNextRoundRequested -= OnSectionPairNextRoundAsync;
            vm.DeleteLastRoundRequested -= OnSectionDeleteLastRoundAsync;
            vm.SoftDeleteRequested -= OnSectionSoftDeleteAsync;
            vm.UndeleteRequested   -= OnSectionUndeleteAsync;
            vm.HardDeleteRequested -= OnSectionHardDeleteAsync;
            vm.PlayerSoftDeleteRequested -= OnPlayerSoftDeleteAsync;
            vm.PlayerUndeleteRequested   -= OnPlayerUndeleteAsync;
            vm.PlayerHardDeleteRequested -= OnPlayerHardDeleteAsync;
            vm.PlayerWithdrawRequested   -= OnPlayerWithdrawAsync;
            vm.PlayerUnwithdrawRequested -= OnPlayerUnwithdrawAsync;
            vm.PlayerManageByesRequested -= OnPlayerManageByesAsync;
            vm.PlayerEditRequested -= OnPlayerEditAsync;
            vm.PlayerAddRequested -= OnPlayerAddAsync;
            vm.PlayerImportRequested -= OnPlayerImportAsync;
            vm.MoveRequested -= OnSectionMoveAsync;
            vm.PairingEngineChangeRequested -= OnSectionPairingEngineChangeAsync;
        }
    }

    private async Task OnSectionPairingEngineChangeAsync(
        SectionViewModel section,
        FreePair.Core.Tournaments.Enums.PairingEngineKind? engine)
    {
        if (Tournament is null) return;
        // No-op when the section already has the requested override;
        // the combobox seeds itself on rebuild and we don't want to
        // round-trip the writer for a no-change.
        var current = Tournament.Sections.FirstOrDefault(s => s.Name == section.Name);
        if (current is null) return;
        if (current.PairingEngine == engine) return;

        try
        {
            Tournament = TournamentMutations.SetSectionPairingEngine(
                Tournament, section.Name, engine);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to change pairing engine for '{section.Name}': {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async void OnSectionResultChanged(
        SectionViewModel section,
        int round,
        PairingRow row,
        PairingResult newResult)
    {
        if (Tournament is null)
        {
            return;
        }

        try
        {
            Tournament = TournamentMutations.SetPairingResult(
                Tournament,
                section.Name,
                round,
                row.WhitePair,
                row.BlackPair,
                newResult);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to record result: {ex.Message}";
            return;
        }

        // Flag for the imminent save so PersistCurrentTournamentAsync
        // knows to run an auto-publish (when the results flag is on).
        _autoPublishResultsPending = true;
        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnSectionDeleteLastRoundAsync(SectionViewModel section)
    {
        if (Tournament is null)
        {
            return;
        }

        if (section.Section.Rounds.Count == 0)
        {
            ErrorMessage = $"{section.Name} has no rounds to delete.";
            return;
        }

        var roundNumber = section.Section.Rounds.Count;
        if (PromptConfirmAsync is not null)
        {
            var confirmed = await PromptConfirmAsync(
                "Delete round",
                $"Delete round {roundNumber} of {section.Name}? " +
                $"This removes all of that round's pairings and results and cannot be undone.",
                $"Delete round {roundNumber}").ConfigureAwait(true);

            if (!confirmed)
            {
                return;
            }
        }

        try
        {
            Tournament = TournamentMutations.DeleteLastRound(Tournament, section.Name);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete round: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnSectionSoftDeleteAsync(SectionViewModel section)
    {
        if (Tournament is null) return;

        if (PromptConfirmAsync is not null)
        {
            var confirmed = await PromptConfirmAsync(
                "Soft-delete section",
                $"Mark '{section.Name}' as soft-deleted? " +
                $"All of the section's data (players, pairings, results, prizes) is kept intact " +
                $"but the section will be locked against further edits and will NOT be included " +
                $"in published results. You can undo this at any time by clicking 'Undelete' on " +
                $"the section.",
                "Soft-delete").ConfigureAwait(true);
            if (!confirmed) return;
        }

        try
        {
            Tournament = TournamentMutations.SoftDeleteSection(Tournament, section.Name);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to soft-delete section: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnSectionUndeleteAsync(SectionViewModel section)
    {
        if (Tournament is null) return;

        try
        {
            Tournament = TournamentMutations.UndeleteSection(Tournament, section.Name);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to undelete section: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnSectionHardDeleteAsync(SectionViewModel section)
    {
        if (Tournament is null) return;

        if (PromptConfirmAsync is not null)
        {
            var confirmed = await PromptConfirmAsync(
                "Permanently delete section",
                $"PERMANENTLY DELETE '{section.Name}'?\n\n" +
                $"Every player, round, pairing, result, and prize in this section will be " +
                $"discarded. This change cannot be undone short of restoring a backup of your " +
                $".sjson file.\n\n" +
                $"Are you sure you want to continue?",
                "Permanently delete").ConfigureAwait(true);
            if (!confirmed) return;
        }

        try
        {
            Tournament = TournamentMutations.HardDeleteSection(Tournament, section.Name);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete section: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnSectionMoveAsync(SectionViewModel section, int delta)
    {
        if (Tournament is null) return;
        var name = section.Name;

        try
        {
            Tournament = TournamentMutations.MoveSection(Tournament, name, delta);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to move section: {ex.Message}";
            return;
        }

        // Re-select the moved section so the TD keeps working in the
        // same context after the nav list rebuilds from the new domain
        // order.
        SelectedSection = Sections.FirstOrDefault(s => s.Name == name);

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    // ================================================================
    // Player lifecycle handlers
    // ================================================================

    private async Task OnPlayerSoftDeleteAsync(SectionViewModel section, int pairNumber)
    {
        if (Tournament is null) return;

        var player = section.Section.Players.FirstOrDefault(p => p.PairNumber == pairNumber);
        var label  = player is null ? $"#{pairNumber}" : $"#{pairNumber} {player.Name}";

        if (PromptConfirmAsync is not null)
        {
            var confirmed = await PromptConfirmAsync(
                "Soft-delete player",
                $"Mark player {label} in '{section.Name}' as soft-deleted?\n\n" +
                $"The player will be excluded from pairings, standings, and published results. " +
                $"All data is preserved — click the ↩ icon to restore them later. " +
                $"This is only allowed before round 1 is paired.",
                "Soft-delete").ConfigureAwait(true);
            if (!confirmed) return;
        }

        try
        {
            Tournament = TournamentMutations.SoftDeletePlayer(Tournament, section.Name, pairNumber);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to soft-delete player: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerUndeleteAsync(SectionViewModel section, int pairNumber)
    {
        if (Tournament is null) return;

        try
        {
            Tournament = TournamentMutations.UndeletePlayer(Tournament, section.Name, pairNumber);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to undelete player: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerHardDeleteAsync(SectionViewModel section, int pairNumber)
    {
        if (Tournament is null) return;

        var player = section.Section.Players.FirstOrDefault(p => p.PairNumber == pairNumber);
        var label  = player is null ? $"#{pairNumber}" : $"#{pairNumber} {player.Name}";

        if (PromptConfirmAsync is not null)
        {
            var confirmed = await PromptConfirmAsync(
                "Permanently delete player",
                $"PERMANENTLY DELETE player {label} from '{section.Name}'?\n\n" +
                $"The player will be removed entirely from this section. This is only " +
                $"allowed before round 1 is paired and cannot be undone short of " +
                $"restoring a backup.",
                "Permanently delete").ConfigureAwait(true);
            if (!confirmed) return;
        }

        try
        {
            Tournament = TournamentMutations.HardDeletePlayer(Tournament, section.Name, pairNumber);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete player: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerWithdrawAsync(SectionViewModel section, int pairNumber)
    {
        if (Tournament is null) return;

        var player = section.Section.Players.FirstOrDefault(p => p.PairNumber == pairNumber);
        var label  = player is null ? $"#{pairNumber}" : $"#{pairNumber} {player.Name}";

        if (PromptConfirmAsync is not null)
        {
            var confirmed = await PromptConfirmAsync(
                "Withdraw player",
                $"Withdraw {label} from '{section.Name}'?\n\n" +
                $"The player's existing game results stay in place and still count " +
                $"toward their opponents' tiebreaks. They won't be paired in any " +
                $"future round. You can reverse this at any time via the undo icon " +
                $"on the Players tab.",
                "Withdraw").ConfigureAwait(true);
            if (!confirmed) return;
        }

        try
        {
            Tournament = TournamentMutations.SetPlayerWithdrawn(Tournament, section.Name, pairNumber, withdrawn: true);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to withdraw player: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerUnwithdrawAsync(SectionViewModel section, int pairNumber)
    {
        if (Tournament is null) return;

        try
        {
            Tournament = TournamentMutations.SetPlayerWithdrawn(Tournament, section.Name, pairNumber, withdrawn: false);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to return player from withdrawal: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerManageByesAsync(SectionViewModel section, int pairNumber)
    {
        if (Tournament is null || ShowManageByesDialogAsync is null) return;

        var player = section.Section.Players.FirstOrDefault(p => p.PairNumber == pairNumber);
        if (player is null) return;

        var dialogVm = new ManageByesViewModel(
            sectionName: section.Name,
            player: player,
            targetRounds: section.TargetRounds,
            roundsPaired: section.Section.RoundsPaired);

        var result = await ShowManageByesDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return; // user cancelled

        var diffs = result.BuildDiffs();
        if (diffs.Count == 0) return;

        try
        {
            foreach (var (round, newKind) in diffs)
            {
                Tournament = newKind is null
                    ? TournamentMutations.RemoveRequestedBye(Tournament, section.Name, pairNumber, round)
                    : TournamentMutations.AddRequestedBye(Tournament, section.Name, pairNumber, round, newKind.Value);
            }
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to apply bye changes: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerEditAsync(SectionViewModel section, int pairNumber)
    {
        if (Tournament is null || ShowPlayerFormDialogAsync is null) return;

        var player = section.Section.Players.FirstOrDefault(p => p.PairNumber == pairNumber);
        if (player is null) return;

        var dialogVm = PlayerFormViewModel.ForEdit(section.Name, player);
        var result = await ShowPlayerFormDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return; // cancelled

        if (!result.TryValidate(out var rating, out var secondaryRating))
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        try
        {
            Tournament = TournamentMutations.UpdatePlayerInfo(
                Tournament,
                section.Name,
                pairNumber,
                name: result.Name,
                uscfId: result.UscfId,
                rating: rating,
                secondaryRating: secondaryRating,
                membershipExpiration: result.MembershipExpiration,
                club: result.Club,
                state: result.State,
                team: result.Team,
                email: result.Email,
                phone: result.Phone);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update player: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerAddAsync(SectionViewModel section)
    {
        if (Tournament is null || ShowPlayerFormDialogAsync is null) return;

        var nextPair = section.Section.Players.Count == 0
            ? 1
            : section.Section.Players.Max(p => p.PairNumber) + 1;
        var dialogVm = PlayerFormViewModel.ForAdd(
            sectionName: section.Name,
            nextPairNumber: nextPair,
            roundsPaired: section.Section.RoundsPaired);

        var result = await ShowPlayerFormDialogAsync(dialogVm).ConfigureAwait(true);
        if (result is null) return; // cancelled

        if (!result.TryValidate(out var rating, out var secondaryRating))
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        try
        {
            Tournament = TournamentMutations.AddPlayer(
                Tournament,
                section.Name,
                name: result.Name,
                uscfId: result.UscfId,
                rating: rating,
                secondaryRating: secondaryRating,
                membershipExpiration: result.MembershipExpiration,
                club: result.Club,
                state: result.State,
                team: result.Team,
                email: result.Email,
                phone: result.Phone,
                byesForPastRounds: result.CollectByesForPastRounds());
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add player: {ex.Message}";
            return;
        }

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnPlayerImportAsync(SectionViewModel section)
    {
        if (Tournament is null || PickPlayerImportFileAsync is null) return;

        var path = await PickPlayerImportFileAsync().ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(path)) return;

        FreePair.Core.Importers.PlayerImportResult result;
        try
        {
            result = FreePair.Core.Importers.PlayerImport.FromFile(path);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to read '{System.IO.Path.GetFileName(path)}': {ex.Message}";
            return;
        }

        if (result.Players.Count == 0)
        {
            ErrorMessage = result.Warnings.Count > 0
                ? $"No players imported. {string.Join(" ", result.Warnings)}"
                : "No players imported.";
            return;
        }

        var added = 0;
        var failed = new System.Collections.Generic.List<string>();
        foreach (var draft in result.Players)
        {
            try
            {
                Tournament = TournamentMutations.AddPlayer(
                    Tournament!, section.Name,
                    name: draft.Name,
                    uscfId: draft.UscfId,
                    rating: draft.Rating,
                    secondaryRating: draft.SecondaryRating,
                    membershipExpiration: draft.MembershipExpiration,
                    club: draft.Club,
                    state: draft.State,
                    team: draft.Team,
                    email: draft.Email,
                    phone: draft.Phone);
                added++;
            }
            catch (Exception ex)
            {
                failed.Add($"{draft.Name}: {ex.Message}");
            }
        }

        // Summarize — short, actionable. Success + warning counts
        // concatenated into the existing ErrorMessage banner.
        var bits = new System.Collections.Generic.List<string>
        {
            $"Imported {added} player{(added == 1 ? "" : "s")} into '{section.Name}'.",
        };
        if (failed.Count > 0)
        {
            bits.Add($"{failed.Count} row(s) failed: {string.Join("; ", failed.Take(3))}" +
                     (failed.Count > 3 ? "; …" : string.Empty));
        }
        if (result.Warnings.Count > 0)
        {
            bits.Add(string.Join(" ", result.Warnings));
        }
        ErrorMessage = string.Join(" ", bits);

        await PersistCurrentTournamentAsync().ConfigureAwait(true);
    }

    private async Task OnSectionPairNextRoundAsync(SectionViewModel section) =>
        await PairSectionNextRoundAsync(section, skipStartingBoardPrompt: false).ConfigureAwait(true);

    /// <summary>
    /// Underlying pairing flow for a single section. Used both by
    /// the per-section "Pair next round" button (with the standard
    /// prompts) and by the batch "Pair all sections" command (with
    /// <paramref name="skipStartingBoardPrompt"/>=<c>true</c> so
    /// the TD isn't dialog-bombed N times for an already-confirmed
    /// batch). Returns when the round has been appended (and
    /// optionally previewed/persisted) — exits early on any user
    /// cancellation in any sub-prompt without partial side-effects.
    /// </summary>
    private async Task PairSectionNextRoundAsync(SectionViewModel section, bool skipStartingBoardPrompt)
    {
        if (Tournament is null)
        {
            return;
        }

        // Gate: any reason the section itself blocks pairing?
        var block = section.PairNextRoundBlockReason;
        if (block is not null)
        {
            ErrorMessage = block;
            return;
        }

        section.IsPairingNextRound = true;
        ErrorMessage = null;

        try
        {
            // Round-robin sections don't need BBP — the Berger
            // schedule is deterministic and computed in-process.
            // Short-circuit before prompting for the engine path or
            // round-1 initial colour.
            if (section.Section.Kind == SectionKind.RoundRobin)
            {
                Tournament = TournamentMutations.AppendRoundRobinRound(
                    Tournament,
                    section.Name);

                var currentRr = SelectedSection;
                if (currentRr is not null)
                {
                    var newRoundRr = currentRr.AvailableRounds
                        .FirstOrDefault(r => r.Number == currentRr.Section.Rounds.Count);
                    if (newRoundRr is not null)
                    {
                        currentRr.SelectedRound = newRoundRr;
                    }
                }

                _autoPublishPairingsPending = true;
                await PersistCurrentTournamentAsync().ConfigureAwait(true);
                return;
            }

            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            var sectionSnapshot = section.Section;
            var tournamentSnapshot = Tournament;

            // Pick the right binary for the section's effective pairing
            // engine. FIDE-rated events default to BBP / FIDE Dutch;
            // everything else defaults to FreePair's USCF engine. The
            // TD can pin either tournament-wide (Tournament.PairingEngine)
            // or per-section (Section.PairingEngine).
            var effectiveEngine = FreePair.Core.Tournaments.PairingEngineDefaults
                .Resolve(tournamentSnapshot, sectionSnapshot);
            var enginePath = FreePair.Core.Bbp.BbpPairingEngine.ResolveEffectivePathFor(
                effectiveEngine,
                settings.PairingEngineBinaryPath,
                settings.UscfEngineBinaryPath);

            // Round 1: ask the TD which colour the top seed should receive.
            // Later rounds: BBP derives colours from history.
            var initialColor = InitialColor.White;
            if (sectionSnapshot.Rounds.Count == 0 && PromptInitialColorAsync is not null)
            {
                var picked = await PromptInitialColorAsync().ConfigureAwait(true);
                if (picked is null)
                {
                    // User cancelled the dialog — abort without an error.
                    return;
                }
                initialColor = picked.Value;
            }

            // Every-round prompt: confirm/override the physical
            // starting board number. Pre-fills with the section's
            // current FirstBoard; the TD can jump to the
            // recommendation or pick anything in [1, 9999]. Cancel
            // aborts pairing without side-effects. If the chosen
            // value differs from the current FirstBoard we
            // SetSectionFirstBoard before pairing so the new offset
            // applies to this round (and persists for the next).
            // Skipped when invoked from batch "Pair all sections"
            // since the TD has already reviewed start boards in
            // that flow.
            if (!skipStartingBoardPrompt && PromptStartingBoardAsync is not null)
            {
                var recommended = BoardNumberRecommender.Recommend(tournamentSnapshot)
                    .TryGetValue(section.Name, out var rec) ? rec : 1;
                var currentFirst = sectionSnapshot.FirstBoard ?? 1;
                var nextRoundNumber = sectionSnapshot.Rounds.Count + 1;

                var chosen = await PromptStartingBoardAsync(
                    section.Name, nextRoundNumber, currentFirst, recommended).ConfigureAwait(true);
                if (chosen is null) return; // cancelled

                if (chosen.Value != currentFirst)
                {
                    var newFirst = chosen.Value <= 1 ? (int?)null : chosen.Value;
                    Tournament = TournamentMutations.SetSectionFirstBoard(
                        Tournament, section.Name, newFirst);
                    tournamentSnapshot = Tournament;
                    sectionSnapshot = tournamentSnapshot.Sections.Single(s => s.Name == section.Name);
                }
            }

            var result = await Task.Run(() =>
                _pairingEngine.GenerateNextRoundAsync(
                    enginePath,
                    tournamentSnapshot,
                    sectionSnapshot,
                    initialColor)).ConfigureAwait(true);

            Tournament = TournamentMutations.AppendRound(
                Tournament,
                section.Name,
                result);

            // Pre-commit preview: let the TD inspect the proposed
            // pairings and intervene (colour swap / board swap / late
            // ½-pt bye) before persisting. The dialog returns either
            // the mutated tournament or null on cancel; on cancel we
            // revert the just-appended round.
            if (PromptPairingPreviewAsync is not null)
            {
                var newRoundNumber = section.Section.Rounds.Count + 1;
                var previewed = await PromptPairingPreviewAsync(
                    Tournament,
                    section.Name,
                    newRoundNumber,
                    result.Conflicts).ConfigureAwait(true);

                if (previewed is null)
                {
                    Tournament = TournamentMutations.DeleteLastRound(
                        Tournament, section.Name);
                    return;
                }
                Tournament = previewed;
            }

            // Auto-select the new round in the rebuilt Section VM.
            var current = SelectedSection;
            if (current is not null)
            {
                var newRound = current.AvailableRounds
                    .FirstOrDefault(r => r.Number == current.Section.Rounds.Count);
                if (newRound is not null)
                {
                    current.SelectedRound = newRound;
                }
            }

            _autoPublishPairingsPending = true;
            await PersistCurrentTournamentAsync().ConfigureAwait(true);
        }
        catch (BbpNotConfiguredException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (BbpExecutionException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Pairing failed: {ex.Message}";
        }
        finally
        {
            // The rebuilt section VM (if the mutation succeeded) was a new
            // object; its IsPairingNextRound is already false. Only clear on
            // the original if rebuild didn't happen (error path).
            section.IsPairingNextRound = false;
        }
    }

    private async Task PersistCurrentTournamentAsync()
    {
        if (Tournament is null || string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            _autoPublishPairingsPending = false;
            _autoPublishResultsPending  = false;
            return;
        }

        await _saveGate.WaitAsync().ConfigureAwait(true);
        try
        {
            SaveStatus = "Saving...";
            await _writer.SaveAsync(CurrentFilePath, Tournament).ConfigureAwait(true);
            LastSavedAt = DateTimeOffset.Now;
            SaveStatus = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to auto-save tournament: {ex.Message}";
            SaveStatus = null;
            _autoPublishPairingsPending = false;
            _autoPublishResultsPending  = false;
            return;
        }
        finally
        {
            _saveGate.Release();
        }

        // After a successful save, consult the session auto-publish
        // flags. We fire-and-forget so the result-entry path stays
        // snappy; any newer save cancels the in-flight upload via
        // _autoPublishCts (last-write-wins).
        var shouldPublish =
            (_autoPublishPairingsPending && AutoPublishPairings) ||
            (_autoPublishResultsPending  && AutoPublishResults);
        _autoPublishPairingsPending = false;
        _autoPublishResultsPending  = false;

        if (shouldPublish)
        {
            _ = AutoPublishAsync();
        }
    }

    /// <summary>
    /// Fire-and-forget auto-publish triggered after an auto-save.
    /// Uses the session <see cref="PublishBaseUrl"/> +
    /// <see cref="Tournament"/>'s event id / passcode. Failures land
    /// in <see cref="ErrorMessage"/>; successes flash a brief
    /// <see cref="SaveStatus"/> note.
    /// </summary>
    private async Task AutoPublishAsync()
    {
        var t = Tournament;
        var path = CurrentFilePath;
        if (t is null || string.IsNullOrWhiteSpace(path)) return;
        if (string.IsNullOrWhiteSpace(t.NachEventId) || string.IsNullOrWhiteSpace(t.NachPasscode))
        {
            ErrorMessage = "Auto-publish skipped — no NACH event ID or passcode set.";
            return;
        }
        if (string.IsNullOrWhiteSpace(PublishBaseUrl)) return;

        // Cancel any in-flight upload from an earlier save.
        _autoPublishCts?.Cancel();
        _autoPublishCts?.Dispose();
        _autoPublishCts = new CancellationTokenSource();
        var ct = _autoPublishCts.Token;

        // Reset the last-published hyperlink + timestamp — re-populated on success.
        LastPublishedUrl = null;
        LastPublishedAt  = null;

        try
        {
            SaveStatus = "Publishing…";

            // Upload the raw .sjson first.
            var result = await _publishingClient.PublishAsync(
                PublishBaseUrl, t.NachEventId!, t.NachPasscode!,
                FileType.SwissSys11SJson,
                path!, ct).ConfigureAwait(true);

            if (ct.IsCancellationRequested) return;

            if (!result.Success)
            {
                ErrorMessage = $"Publish failed: {result.ErrorMessage ?? "Unknown error."}";
                SaveStatus = null;
                return;
            }

            // Then upload the derived results/pairings JSON that
            // NAChessHub uses to render public pages. Written next to
            // the .sjson (NAChessHub naming convention) so the TD can
            // inspect it after the fact — kept on disk, not deleted.
            var derivedPath = PublishingDialogViewModel.DeriveResultJsonPath(path!);
            await System.IO.File.WriteAllTextAsync(
                derivedPath,
                SwissSysResultJsonBuilder.Build(t),
                ct).ConfigureAwait(true);

            var result2 = await _publishingClient.PublishAsync(
                PublishBaseUrl, t.NachEventId!, t.NachPasscode!,
                FileType.SwissSysJSON,
                derivedPath, ct).ConfigureAwait(true);

            if (ct.IsCancellationRequested) return;

            if (result2.Success)
            {
                SaveStatus = $"Published to {_publishingClient.DisplayName}.";
                var root = (PublishBaseUrl ?? "").TrimEnd('/');
                LastPublishedUrl = $"{root}/EventFiles?EventID={System.Uri.EscapeDataString(t.NachEventId!)}";
                LastPublishedAt  = DateTimeOffset.Now;
                await StampLastPublishedAtAsync(LastPublishedAt.Value).ConfigureAwait(true);
            }
            else
            {
                ErrorMessage = $"Publish (results JSON) failed: {result2.ErrorMessage ?? "Unknown error."} (see {derivedPath})";
                SaveStatus = null;
            }
        }
        catch (OperationCanceledException) { /* superseded by a newer save */ }
        catch (Exception ex)
        {
            ErrorMessage = $"Publish failed: {ex.Message}";
            SaveStatus = null;
        }
    }

    /// <summary>
    /// Stamps <see cref="Tournament.LastPublishedAt"/> with the given
    /// timestamp and writes the tournament directly via the writer so
    /// the <c>"FreePair last published at"</c> Overview key persists
    /// across restarts. Saves under <c>_saveGate</c> to serialise with
    /// auto-saves. Failures here are non-fatal — the in-session
    /// toolbar label still renders; only persistence is lost.
    /// </summary>
    private async Task StampLastPublishedAtAsync(DateTimeOffset at)
    {
        if (Tournament is null || string.IsNullOrWhiteSpace(CurrentFilePath)) return;
        try
        {
            Tournament = TournamentMutations.SetTournamentInfo(
                Tournament,
                lastPublishedAt: new Box<DateTimeOffset?>(at));
            await _saveGate.WaitAsync().ConfigureAwait(true);
            try
            {
                await _writer.SaveAsync(CurrentFilePath!, Tournament).ConfigureAwait(true);
                LastSavedAt = DateTimeOffset.Now;
            }
            finally
            {
                _saveGate.Release();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not stamp publish timestamp: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens the Publish dialog, then copies the TD's choices (URL +
    /// auto-flags + edited event id/passcode) back onto the VM /
    /// tournament. Noop when the view hasn't wired
    /// <see cref="ShowPublishingDialogAsync"/> or no tournament is
    /// loaded.
    /// </summary>
    [RelayCommand]
    private async Task PublishAsync()
    {
        if (Tournament is null || ShowPublishingDialogAsync is null) return;

        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        var clients = new Dictionary<string, IPublishingClient>
        {
            ["nachesshub"] = _publishingClient,
        };

        var vm = new PublishingDialogViewModel(
            clients,
            getTournament:         () => Tournament,
            getTournamentFilePath: () => CurrentFilePath,
            baseUrlDefault:        string.IsNullOrWhiteSpace(PublishBaseUrl)
                                       ? settings.NaChessHubBaseUrl
                                       : PublishBaseUrl,
            autoPublishPairingsDefault: AutoPublishPairings,
            autoPublishResultsDefault:  AutoPublishResults,
            onPublishSucceeded:    async ts =>
            {
                // Mirror the auto-publish path's bookkeeping: populate
                // the toolbar label + persist the timestamp onto the
                // tournament so it survives app restart.
                LastPublishedAt  = ts;
                var root = (PublishBaseUrl ?? "").TrimEnd('/');
                if (Tournament?.NachEventId is { } eid)
                {
                    LastPublishedUrl = $"{root}/EventFiles?EventID={System.Uri.EscapeDataString(eid)}";
                }
                await StampLastPublishedAtAsync(ts).ConfigureAwait(true);
            });

        var result = await ShowPublishingDialogAsync(vm).ConfigureAwait(true);
        if (result is null) return;

        // Pull edits back onto the session state.
        PublishBaseUrl      = result.BaseUrl ?? PublishBaseUrl;
        AutoPublishPairings = result.AutoPublishPairings;
        AutoPublishResults  = result.AutoPublishResults;

        // Event ID / passcode are edited on the Event tab, not here —
        // only the two auto-flags round-trip through the Overview. Fold
        // them onto the tournament and trigger an auto-save so the
        // FreePair keys persist right away.
        if (Tournament is not null)
        {
            Tournament = TournamentMutations.SetTournamentInfo(
                Tournament,
                autoPublishPairings: new Box<bool?>(AutoPublishPairings),
                autoPublishResults:  new Box<bool?>(AutoPublishResults));

            await PersistCurrentTournamentAsync().ConfigureAwait(true);
        }
    }

    private async Task LoadAsync(string filePath)
    {
        IsLoading = true;
        ErrorMessage = null;

        // Per-file exclusive lock: refuses to load if another FreePair
        // instance has the same .sjson open. Without this, two
        // instances would auto-save into each other and silently
        // clobber edits. The lock is released when the tournament is
        // closed (Close), replaced by another LoadAsync, or the
        // process exits.
        var newLock = FreePair.Core.Tournaments.TournamentLock.TryAcquire(filePath);
        if (newLock is null)
        {
            IsLoading = false;
            ErrorMessage =
                $"'{System.IO.Path.GetFileName(filePath)}' is already open in another FreePair window. " +
                "Switch to that window, or close it first.";
            return;
        }
        _currentFileLock?.Dispose();
        _currentFileLock = newLock;

        try
        {
            var tournament = await _loader.LoadAsync(filePath).ConfigureAwait(true);
            Tournament = tournament;
            CurrentFilePath = filePath;

            // Seed session publishing state from app-wide defaults.
            // Per-tournament auto-flags override when the .sjson
            // carried them (FreePair auto publish pairings/results
            // keys in the Overview block); otherwise we inherit the
            // app-wide defaults from Settings.
            var pubSettings = await _settingsService.LoadAsync().ConfigureAwait(true);
            PublishBaseUrl      = pubSettings.NaChessHubBaseUrl;
            AutoPublishPairings = tournament.AutoPublishPairings ?? pubSettings.AutoPublishPairingsDefault;
            AutoPublishResults  = tournament.AutoPublishResults  ?? pubSettings.AutoPublishResultsDefault;

            // Seed the "last published" toolbar label from the persisted
            // timestamp, if any. The URL is rebuilt from the current
            // PublishBaseUrl + the tournament's NACH event id so it
            // follows any URL change the TD has made since the last
            // publish. Only populated when both pieces of info exist.
            if (tournament.LastPublishedAt is { } ts
                && !string.IsNullOrWhiteSpace(tournament.NachEventId))
            {
                LastPublishedAt  = ts;
                var root = PublishBaseUrl.TrimEnd('/');
                LastPublishedUrl = $"{root}/EventFiles?EventID={System.Uri.EscapeDataString(tournament.NachEventId!)}";
            }
            else
            {
                LastPublishedAt  = null;
                LastPublishedUrl = null;
            }

            // Seed the "last saved at…" label from the file's on-disk
            // last-write time so the TD sees a sensible value right
            // away — before they've triggered any auto-save. Any
            // subsequent in-session save overwrites this with the
            // wall-clock time of that save.
            try
            {
                LastSavedAt = new DateTimeOffset(File.GetLastWriteTime(filePath));
            }
            catch
            {
                LastSavedAt = null;
            }

            await PersistLastPathAsync(filePath).ConfigureAwait(true);
        }
        catch (FileNotFoundException)
        {
            Tournament = null;
            ErrorMessage = $"File not found: {filePath}";
        }
        catch (Exception ex)
        {
            Tournament = null;
            ErrorMessage = $"Could not open tournament: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PersistLastPathAsync(string path)
    {
        try
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            settings.LastTournamentFilePath = path;
            await _settingsService.SaveAsync(settings).ConfigureAwait(true);
        }
        catch
        {
            // Non-fatal — the tournament is already loaded; persistence is
            // best-effort.
        }
    }
}
