using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Bbp;
using FreePair.Core.Formatting;
using FreePair.Core.Settings;
using FreePair.Core.Updates;

namespace FreePair.App.ViewModels;

/// <summary>
/// View model backing the application settings screen. Responsible for
/// loading/saving <see cref="AppSettings"/> and validating the paths the
/// user supplies.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IScoreFormatter? _formatter;
    private AppSettings _settings = new();
    private bool _suppressFormatterUpdate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PairingEngineBinaryExists))]
    [NotifyPropertyChangedFor(nameof(PairingEngineBinaryMissing))]
    [NotifyPropertyChangedFor(nameof(HasPairingEngineBinaryPath))]
    private string? _pairingEngineBinaryPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UscfEngineBinaryExists))]
    [NotifyPropertyChangedFor(nameof(UscfEngineBinaryMissing))]
    [NotifyPropertyChangedFor(nameof(HasUscfEngineBinaryPath))]
    private string? _uscfEngineBinaryPath;

    [ObservableProperty]
    private bool _useAsciiOnly = true;

    // ============ Online publishing defaults ============

    [ObservableProperty] private string _naChessHubBaseUrl = "https://nachesshub.com";
    [ObservableProperty] private bool _autoPublishPairingsDefault;
    [ObservableProperty] private bool _autoPublishResultsDefault;

    // ============ Auto-update ============

    /// <summary>
    /// Mirror of <see cref="AppSettings.CheckForUpdatesOnStartup"/>;
    /// drives the "Check for updates on startup" checkbox in the
    /// Settings view. Persisted to settings.json on Save.
    /// </summary>
    [ObservableProperty] private bool _checkForUpdatesOnStartup = true;

    /// <summary>
    /// Mirror of <see cref="AppSettings.UpdateFeedRepoUrl"/>; full
    /// URL of the GitHub repo to poll for releases. Forks override.
    /// </summary>
    [ObservableProperty] private string _updateFeedRepoUrl = "https://github.com/freepairchess/FreePair";

    /// <summary>
    /// Mirror of <see cref="AppSettings.UpdateIncludePreReleases"/>;
    /// when on, the update check considers pre-release tags. Off
    /// by default — stable-channel TDs don't want preview builds.
    /// </summary>
    [ObservableProperty] private bool _updateIncludePreReleases;

    /// <summary>
    /// Current FreePair build version, surfaced in the Settings UI so
    /// TDs can confirm what's installed before / after an update.
    /// Read from the assembly's <see cref="AssemblyInformationalVersionAttribute"/>
    /// (set by Velopack at build time) with a fallback to
    /// <see cref="AssemblyFileVersionAttribute"/> for dev builds.
    /// </summary>
    public string CurrentVersion { get; } =
        typeof(SettingsViewModel).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(SettingsViewModel).Assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
        ?? "dev";

    /// <summary>
    /// Status / result of the most recent manual update check fired
    /// from the Settings page. Empty until the user clicks "Check now".
    /// </summary>
    [ObservableProperty] private string? _updateCheckStatus;

    /// <summary>
    /// True while an on-demand update check is in flight, so the
    /// Settings UI can disable the button and show a "Checking..."
    /// affordance instead of leaving it click-spammable.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesNowCommand))]
    private bool _isCheckingForUpdates;

    private IUpdateService? _updateService;
    private Func<Task>? _hostCheckForUpdates;

    /// <summary>
    /// Wires the platform-specific update service so the Settings page
    /// can drive its "Check now" button. Called once from the host
    /// after the same service is attached to <see cref="MainWindowViewModel"/>.
    /// The <paramref name="hostCheck"/> delegate is invoked alongside
    /// the local check so the main-window banner stays in sync with
    /// what the Settings status shows.
    /// </summary>
    public void AttachUpdateService(IUpdateService service, Func<Task>? hostCheck = null)
    {
        _updateService = service ?? throw new ArgumentNullException(nameof(service));
        _hostCheckForUpdates = hostCheck;
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdatesNow))]
    private async Task CheckForUpdatesNowAsync()
    {
        if (_updateService is null)
        {
            UpdateCheckStatus = "Updater not available in this build (dev / portable run).";
            return;
        }

        IsCheckingForUpdates = true;
        try
        {
            UpdateCheckStatus = "Checking GitHub Releases...";
            var result = await _updateService.CheckAsync().ConfigureAwait(true);
            UpdateCheckStatus = result switch
            {
                UpdateCheckResult.Available a    => $"Update available: v{a.Version}. See the banner at the top of the window.",
                UpdateCheckResult.UpToDate       => $"FreePair is up to date (v{CurrentVersion}).",
                UpdateCheckResult.NotInstalled   => "Updater not available in this build (dev / portable run).",
                UpdateCheckResult.Failed f       => $"Update check failed: {f.Message}",
                _                                => "Unknown update-check result.",
            };

            // Keep the main-window banner in sync — the host check uses
            // the same service so it'll see the same Available / UpToDate
            // outcome and surface the banner / clear it accordingly.
            if (_hostCheckForUpdates is not null)
            {
                await _hostCheckForUpdates().ConfigureAwait(true);
            }
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private bool CanCheckForUpdatesNow() => !IsCheckingForUpdates;

    // ============ Tournament file layout ============

    /// <summary>
    /// Root folder under which FreePair creates per-event
    /// subfolders. Blank ? fall back to
    /// <see cref="FreePair.Core.Tournaments.TournamentFolder.DefaultRoot"/>
    /// (Documents/FreePairEvents).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveTournamentsRootFolder))]
    private string? _tournamentsRootFolder;

    /// <summary>
    /// The computed effective root folder the resolver will use on
    /// disk — either <see cref="TournamentsRootFolder"/> verbatim or
    /// the built-in default when that's blank. Surfaced in the UI so
    /// the TD can always see where events will land.
    /// </summary>
    public string EffectiveTournamentsRootFolder =>
        string.IsNullOrWhiteSpace(TournamentsRootFolder)
            ? FreePair.Core.Tournaments.TournamentFolder.DefaultRoot
            : TournamentsRootFolder!.Trim();

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Creates a design-time instance that uses the default on-disk settings
    /// service. Production code should resolve this view model via DI.
    /// </summary>
    public SettingsViewModel()
        : this(new SettingsService(), null)
    {
    }

    public SettingsViewModel(ISettingsService settingsService, IScoreFormatter? formatter)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _formatter = formatter;
    }

    /// <summary>
    /// Callback used by <see cref="BrowsePairingEngineCommand"/> to let the
    /// view show a native file picker. Returns the selected absolute path, or
    /// <c>null</c> if the user cancelled.
    /// </summary>
    public Func<Task<string?>>? PickPairingEngineBinaryAsync { get; set; }

    /// <summary>
    /// Callback used by <see cref="BrowseUscfEngineCommand"/> — same shape
    /// as <see cref="PickPairingEngineBinaryAsync"/>, just for the USCF
    /// engine binary slot.
    /// </summary>
    public Func<Task<string?>>? PickUscfEngineBinaryAsync { get; set; }

    /// <summary>
    /// Callback used by <see cref="BrowseTournamentsRootFolderCommand"/>
    /// to let the view show a native folder picker. Returns the
    /// selected absolute path, or <c>null</c> if the user cancelled.
    /// </summary>
    public Func<Task<string?>>? PickTournamentsRootFolderAsync { get; set; }

    /// <summary>
    /// Invoked whenever <see cref="UseAsciiOnly"/> changes (after the
    /// formatter has been updated). The host wires this up to refresh
    /// open views so the change is immediately visible.
    /// </summary>
    public Action? FormatPreferenceChanged { get; set; }

    public bool HasPairingEngineBinaryPath => !string.IsNullOrWhiteSpace(PairingEngineBinaryPath);

    /// <summary>
    /// True when <see cref="PairingEngineBinaryPath"/> refers to an existing
    /// file on disk. Surfaced in the view to warn the user about stale paths.
    /// </summary>
    public bool PairingEngineBinaryExists =>
        HasPairingEngineBinaryPath && File.Exists(PairingEngineBinaryPath);

    /// <summary>
    /// True when a path has been provided but no file exists there. Used by
    /// the view to show a validation warning.
    /// </summary>
    public bool PairingEngineBinaryMissing =>
        HasPairingEngineBinaryPath && !File.Exists(PairingEngineBinaryPath);

    public bool HasUscfEngineBinaryPath => !string.IsNullOrWhiteSpace(UscfEngineBinaryPath);

    /// <summary>True when the configured USCF engine path resolves to a real file.</summary>
    public bool UscfEngineBinaryExists =>
        HasUscfEngineBinaryPath && File.Exists(UscfEngineBinaryPath);

    /// <summary>True when the configured USCF engine path is non-empty but does not exist.</summary>
    public bool UscfEngineBinaryMissing =>
        HasUscfEngineBinaryPath && !File.Exists(UscfEngineBinaryPath);

    /// <summary>
    /// Loads persisted settings into the view model. Call once after the view
    /// is ready.
    /// </summary>
    public async Task InitializeAsync()
    {
        _settings = await _settingsService.LoadAsync().ConfigureAwait(true);

        _suppressFormatterUpdate = true;
        try
        {
            PairingEngineBinaryPath = _settings.PairingEngineBinaryPath;
            UscfEngineBinaryPath    = _settings.UscfEngineBinaryPath;

            // Auto-detect bundled engines when no explicit path is configured.
            if (string.IsNullOrWhiteSpace(PairingEngineBinaryPath))
            {
                var bundledBbp = Path.Combine(AppContext.BaseDirectory, BbpPairingEngine.BundledExeName);
                if (File.Exists(bundledBbp))
                    PairingEngineBinaryPath = bundledBbp;
            }
            if (string.IsNullOrWhiteSpace(UscfEngineBinaryPath))
            {
                var bundledUscf = Path.Combine(AppContext.BaseDirectory, BbpPairingEngine.UscfBundledExeName);
                if (File.Exists(bundledUscf))
                    UscfEngineBinaryPath = bundledUscf;
            }
            UseAsciiOnly = _settings.UseAsciiOnly;
            NaChessHubBaseUrl          = _settings.NaChessHubBaseUrl;
            AutoPublishPairingsDefault = _settings.AutoPublishPairingsDefault;
            AutoPublishResultsDefault  = _settings.AutoPublishResultsDefault;
            TournamentsRootFolder      = _settings.TournamentsRootFolder;
            CheckForUpdatesOnStartup   = _settings.CheckForUpdatesOnStartup;
            UpdateFeedRepoUrl          = _settings.UpdateFeedRepoUrl;
            UpdateIncludePreReleases   = _settings.UpdateIncludePreReleases;
        }
        finally
        {
            _suppressFormatterUpdate = false;
        }

        if (_formatter is not null)
        {
            _formatter.UseAsciiOnly = _settings.UseAsciiOnly;
        }

        StatusMessage = null;
    }

    /// <summary>
    /// Returns whether the TD has previously dismissed the
    /// first-run "FreePair uses BBP / FIDE Dutch" disclosure with
    /// "Don't show this again" checked. <c>InitializeAsync</c>
    /// must be called first; otherwise this returns the default
    /// (<c>false</c>) and the dialog will appear on next call.
    /// </summary>
    public bool HasAcknowledgedPairingEngineNotice =>
        _settings?.HasAcknowledgedPairingEngineNotice ?? false;

    /// <summary>
    /// Persists the TD's choice from the disclosure dialog. Pass
    /// <paramref name="dontShowAgain"/>=<c>true</c> to suppress
    /// the dialog on subsequent launches; <c>false</c> to keep
    /// showing it (returning user re-acknowledgement each launch).
    /// </summary>
    public async Task SetPairingEngineNoticeAcknowledgedAsync(bool dontShowAgain)
    {
        _settings ??= await _settingsService.LoadAsync().ConfigureAwait(true);
        if (_settings.HasAcknowledgedPairingEngineNotice == dontShowAgain) return;
        _settings.HasAcknowledgedPairingEngineNotice = dontShowAgain;
        await _settingsService.SaveAsync(_settings).ConfigureAwait(true);
    }

    partial void OnUseAsciiOnlyChanged(bool value)
    {
        if (_suppressFormatterUpdate)
        {
            return;
        }

        if (_formatter is not null)
        {
            _formatter.UseAsciiOnly = value;
        }

        FormatPreferenceChanged?.Invoke();
    }

    [RelayCommand]
    private async Task BrowsePairingEngineAsync()
    {
        if (PickPairingEngineBinaryAsync is null)
        {
            return;
        }

        var picked = await PickPairingEngineBinaryAsync().ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(picked))
        {
            PairingEngineBinaryPath = picked;
        }
    }

    [RelayCommand]
    private async Task BrowseUscfEngineAsync()
    {
        if (PickUscfEngineBinaryAsync is null)
        {
            return;
        }

        var picked = await PickUscfEngineBinaryAsync().ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(picked))
        {
            UscfEngineBinaryPath = picked;
        }
    }

    [RelayCommand]
    private async Task BrowseTournamentsRootFolderAsync()
    {
        if (PickTournamentsRootFolderAsync is null)
        {
            return;
        }

        var picked = await PickTournamentsRootFolderAsync().ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(picked))
        {
            TournamentsRootFolder = picked;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _settings.PairingEngineBinaryPath = string.IsNullOrWhiteSpace(PairingEngineBinaryPath)
            ? null
            : PairingEngineBinaryPath;
        _settings.UscfEngineBinaryPath    = string.IsNullOrWhiteSpace(UscfEngineBinaryPath)
            ? null
            : UscfEngineBinaryPath;
        _settings.UseAsciiOnly = UseAsciiOnly;
        _settings.NaChessHubBaseUrl          = string.IsNullOrWhiteSpace(NaChessHubBaseUrl)
            ? "https://nachesshub.com" : NaChessHubBaseUrl.Trim();
        _settings.AutoPublishPairingsDefault = AutoPublishPairingsDefault;
        _settings.AutoPublishResultsDefault  = AutoPublishResultsDefault;
        _settings.TournamentsRootFolder      = string.IsNullOrWhiteSpace(TournamentsRootFolder)
            ? null : TournamentsRootFolder!.Trim();
        _settings.CheckForUpdatesOnStartup   = CheckForUpdatesOnStartup;
        _settings.UpdateFeedRepoUrl          = string.IsNullOrWhiteSpace(UpdateFeedRepoUrl)
            ? "https://github.com/freepairchess/FreePair" : UpdateFeedRepoUrl.Trim();
        _settings.UpdateIncludePreReleases   = UpdateIncludePreReleases;

        try
        {
            await _settingsService.SaveAsync(_settings).ConfigureAwait(true);

            if (HasPairingEngineBinaryPath && !PairingEngineBinaryExists)
            {
                StatusMessage = "Saved. Warning: the pairing engine binary path does not exist on disk.";
            }
            else
            {
                StatusMessage = "Settings saved.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save settings: {ex.Message}";
        }
    }
}
