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
    public async Task InitializeAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            _formatter.UseAsciiOnly = settings.UseAsciiOnly;

            var path = settings.LastTournamentFilePath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                await LoadAsync(path).ConfigureAwait(true);
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
            await LoadAsync(picked).ConfigureAwait(true);
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
        Tournament = tournament;
        CurrentFilePath = path;
        LastSavedAt = null;
        LastPublishedAt = null;
        LastPublishedUrl = null;
        ErrorMessage = null;
        await PersistCurrentTournamentAsync().ConfigureAwait(true);
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
        }
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

    private async Task OnSectionPairNextRoundAsync(SectionViewModel section)
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
            var enginePath = settings.PairingEngineBinaryPath;
            var sectionSnapshot = section.Section;
            var tournamentSnapshot = Tournament;

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
