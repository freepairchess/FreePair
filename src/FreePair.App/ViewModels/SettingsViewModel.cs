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
    private async Task SaveAsync()
    {
        _settings.PairingEngineBinaryPath = string.IsNullOrWhiteSpace(PairingEngineBinaryPath)
            ? null
            : PairingEngineBinaryPath;
        _settings.UseAsciiOnly = UseAsciiOnly;

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
