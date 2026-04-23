using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Bbp;
using FreePair.Core.Formatting;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTournament))]
    private Tournament? _tournament;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSection))]
    private SectionViewModel? _selectedSection;

    [ObservableProperty]
    private IReadOnlyList<SectionViewModel> _sections = Array.Empty<SectionViewModel>();

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public TournamentViewModel()
        : this(new TournamentLoader(), new SettingsService(), new ScoreFormatter(), new BbpPairingEngine(), new SwissSysTournamentWriter())
    {
    }

    public TournamentViewModel(
        ITournamentLoader loader,
        ISettingsService settingsService,
        IScoreFormatter formatter,
        IBbpPairingEngine pairingEngine,
        ITournamentWriter writer)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _pairingEngine = pairingEngine ?? throw new ArgumentNullException(nameof(pairingEngine));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
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

    [RelayCommand]
    private void Close()
    {
        Tournament = null;
        CurrentFilePath = null;
        ErrorMessage = null;
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
        vm.ResultChanged += OnSectionResultChanged;
        vm.PairNextRoundRequested += OnSectionPairNextRoundAsync;
        vm.DeleteLastRoundRequested += OnSectionDeleteLastRoundAsync;
    }

    private void DetachSectionEvents()
    {
        foreach (var vm in Sections)
        {
            vm.ResultChanged -= OnSectionResultChanged;
            vm.PairNextRoundRequested -= OnSectionPairNextRoundAsync;
            vm.DeleteLastRoundRequested -= OnSectionDeleteLastRoundAsync;
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
            return;
        }

        await _saveGate.WaitAsync().ConfigureAwait(true);
        try
        {
            SaveStatus = "Saving...";
            await _writer.SaveAsync(CurrentFilePath, Tournament).ConfigureAwait(true);
            SaveStatus = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to auto-save tournament: {ex.Message}";
            SaveStatus = null;
        }
        finally
        {
            _saveGate.Release();
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
