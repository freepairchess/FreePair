using System;
using System.IO;
using System.Threading.Tasks;
using FreePair.Core.Settings;

namespace FreePair.Core.Tests.Settings;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FreePair.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task LoadAsync_returns_default_when_file_missing()
    {
        var service = new SettingsService(_settingsPath);

        var settings = await service.LoadAsync();

        Assert.NotNull(settings);
        Assert.Null(settings.PairingEngineBinaryPath);
    }

    [Fact]
    public async Task SaveAsync_then_LoadAsync_roundtrips_values()
    {
        var service = new SettingsService(_settingsPath);
        var original = new AppSettings
        {
            PairingEngineBinaryPath = @"C:\engines\bbp.exe",
            LastTournamentFilePath = @"C:\tournaments\spring-open.sjson",
            UseAsciiOnly = false,
        };

        await service.SaveAsync(original);
        var loaded = await service.LoadAsync();

        Assert.Equal(original.PairingEngineBinaryPath, loaded.PairingEngineBinaryPath);
        Assert.Equal(original.LastTournamentFilePath, loaded.LastTournamentFilePath);
        Assert.False(loaded.UseAsciiOnly);
    }

    [Fact]
    public async Task LoadAsync_defaults_UseAsciiOnly_to_true()
    {
        var service = new SettingsService(_settingsPath);

        var loaded = await service.LoadAsync();

        Assert.True(loaded.UseAsciiOnly);
    }

    [Fact]
    public async Task SaveAsync_creates_missing_directory()
    {
        var nestedPath = Path.Combine(_tempDir, "nested", "dir", "settings.json");
        var service = new SettingsService(nestedPath);

        await service.SaveAsync(new AppSettings { PairingEngineBinaryPath = "x" });

        Assert.True(File.Exists(nestedPath));
    }

    [Fact]
    public async Task LoadAsync_returns_default_when_file_is_corrupt()
    {
        await File.WriteAllTextAsync(_settingsPath, "{ not valid json");
        var service = new SettingsService(_settingsPath);

        var settings = await service.LoadAsync();

        Assert.NotNull(settings);
        Assert.Null(settings.PairingEngineBinaryPath);
    }

    [Fact]
    public async Task SaveAsync_throws_on_null_settings()
    {
        var service = new SettingsService(_settingsPath);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveAsync(null!));
    }

    [Fact]
    public void Constructor_throws_on_empty_path()
    {
        Assert.Throws<ArgumentException>(() => new SettingsService(""));
        Assert.Throws<ArgumentException>(() => new SettingsService("   "));
    }

    [Fact]
    public void Default_constructor_points_at_ApplicationData_FreePair_settings_json()
    {
        var service = new SettingsService();

        var expectedRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Assert.StartsWith(expectedRoot, service.SettingsFilePath);
        Assert.EndsWith(Path.Combine("FreePair", "settings.json"), service.SettingsFilePath);
    }
}
