using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Updates;

namespace FreePair.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private IUpdateService? _updateService;

    public MainWindowViewModel()
        : this(new TournamentViewModel(), new SettingsViewModel())
    {
    }

    public MainWindowViewModel(TournamentViewModel tournament, SettingsViewModel settings)
    {
        Tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // When the user flips the ASCII / Unicode toggle, rebuild the
        // Section view models so the change is visible immediately.
        Settings.FormatPreferenceChanged = Tournament.RebuildSections;

        // Re-emit WindowTitle whenever the nested tournament loads,
        // closes, or has its overview Title edited via the event
        // config dialog.
        Tournament.PropertyChanged += OnTournamentPropertyChanged;
    }

    public string Greeting { get; } = "FreePair";

    public TournamentViewModel Tournament { get; }

    public SettingsViewModel Settings { get; }

    // ============ Auto-update banner ============

    /// <summary>
    /// SemVer string of the newest published release found by
    /// <see cref="CheckForUpdatesAsync"/>. <c>null</c> when no
    /// check has run yet, FreePair is up-to-date, or the check
    /// failed. Bound by the main window's update banner via
    /// <see cref="HasUpdateAvailable"/>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdateAvailable))]
    private string? _availableUpdateVersion;

    /// <summary>
    /// Markdown release notes for the available update, surfaced
    /// in the banner's tooltip / details pane. Empty when the
    /// release was published without notes.
    /// </summary>
    [ObservableProperty] private string? _availableUpdateNotes;

    /// <summary>
    /// Status text for the Help → Check for updates… menu flow:
    /// "Checking..." / "Up to date" / "Update failed: ...". Cleared
    /// by the next check.
    /// </summary>
    [ObservableProperty] private string? _updateStatusMessage;

    /// <summary>True when an update banner should be shown.</summary>
    public bool HasUpdateAvailable => !string.IsNullOrEmpty(AvailableUpdateVersion);

    /// <summary>
    /// Wires the platform-specific update service. Call once
    /// after construction (e.g. from MainWindow.OnOpened). Pure
    /// VM stays platform-agnostic by accepting an interface.
    /// </summary>
    public void AttachUpdateService(IUpdateService service)
    {
        _updateService = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Polls the update feed and surfaces results in the banner /
    /// status text. Called automatically on startup when
    /// <see cref="AppSettings.CheckForUpdatesOnStartup"/> is on,
    /// and on demand from the Help → Check for updates… menu.
    /// </summary>
    [RelayCommand]
    public async Task CheckForUpdatesAsync()
    {
        if (_updateService is null)
        {
            UpdateStatusMessage = "Updater not available in this build.";
            return;
        }
        UpdateStatusMessage = "Checking for updates...";
        var result = await _updateService.CheckAsync().ConfigureAwait(true);
        switch (result)
        {
            case UpdateCheckResult.Available a:
                AvailableUpdateVersion = a.Version;
                AvailableUpdateNotes   = a.ReleaseNotes;
                UpdateStatusMessage    = $"Update available: v{a.Version}";
                break;
            case UpdateCheckResult.UpToDate:
                AvailableUpdateVersion = null;
                AvailableUpdateNotes   = null;
                UpdateStatusMessage    = "FreePair is up to date.";
                break;
            case UpdateCheckResult.NotInstalled:
                AvailableUpdateVersion = null;
                UpdateStatusMessage    = "Running outside an installed package — auto-update disabled.";
                break;
            case UpdateCheckResult.Failed f:
                AvailableUpdateVersion = null;
                UpdateStatusMessage    = $"Update check failed: {f.Message}";
                break;
        }
    }

    /// <summary>
    /// Downloads and applies the available update, then exits
    /// FreePair so Velopack's launcher can swap binaries. The
    /// tournament VM auto-saves before we hand off.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasUpdateAvailable))]
    public async Task ApplyUpdateAsync()
    {
        if (_updateService is null) return;
        UpdateStatusMessage = "Downloading update...";
        // Note: TDs are expected to have saved (or auto-save has
        // flushed) before clicking Apply Update. The banner copy
        // tells them so. We don't force a save here because
        // PersistCurrentTournamentAsync is internal to TournamentVM
        // and a save failure shouldn't block the update either way.
        try
        {
            await _updateService.ApplyAndRestartAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"Update failed: {ex.Message}";
        }
    }

    // ============ Window title ============

    /// <summary>
    /// Text shown in the OS window title bar. "FreePair" when no
    /// tournament is loaded; "FreePair — {Title}" when one is open.
    /// Falls back to the file name (sans .sjson) for untitled
    /// tournaments so TDs running multiple instances can still tell
    /// them apart.
    /// </summary>
    public string WindowTitle
    {
        get
        {
            var t = Tournament.Tournament;
            if (t is null) return "FreePair";

            var label = !string.IsNullOrWhiteSpace(t.Title)
                ? t.Title!
                : !string.IsNullOrWhiteSpace(Tournament.CurrentFilePath)
                    ? System.IO.Path.GetFileNameWithoutExtension(Tournament.CurrentFilePath)!
                    : "(untitled)";
            return $"FreePair — {label}";
        }
    }

    private void OnTournamentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Tournament loaded / closed → re-bind title. The Tournament
        // record itself is immutable so Overview title edits always
        // go through a 'Tournament = ... with { Title = ... }'
        // assignment, which raises PropertyChanged on the Tournament
        // property — no need to listen per-field.
        if (e.PropertyName is nameof(TournamentViewModel.Tournament)
                           or nameof(TournamentViewModel.CurrentFilePath))
        {
            OnPropertyChanged(nameof(WindowTitle));
        }
    }
}
