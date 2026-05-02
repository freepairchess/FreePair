using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Formatting;
using FreePair.Core.Settings;

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
