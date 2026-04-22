using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Settings;

/// <summary>
/// Default <see cref="ISettingsService"/> implementation that serializes
/// <see cref="AppSettings"/> as JSON to the per-user application data folder.
/// </summary>
public class SettingsService : ISettingsService
{
    private const string AppFolderName = "FreePair";
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public SettingsService()
        : this(GetDefaultSettingsFilePath())
    {
    }

    public SettingsService(string settingsFilePath)
    {
        if (string.IsNullOrWhiteSpace(settingsFilePath))
        {
            throw new ArgumentException("Settings file path must be provided.", nameof(settingsFilePath));
        }

        _settingsFilePath = settingsFilePath;
    }

    /// <summary>
    /// Gets the absolute path of the file that backs the settings store.
    /// </summary>
    public string SettingsFilePath => _settingsFilePath;

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream, s_jsonOptions, cancellationToken).ConfigureAwait(false);
            return settings ?? new AppSettings();
        }
        catch (JsonException)
        {
            // Corrupt settings file – fall back to defaults rather than crashing.
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string GetDefaultSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create);
        return Path.Combine(appData, AppFolderName, SettingsFileName);
    }
}
