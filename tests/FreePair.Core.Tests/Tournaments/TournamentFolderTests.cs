using System.IO;
using FreePair.Core.Settings;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for <see cref="TournamentFolder"/> — the helper that maps a
/// tournament title to a per-event folder on disk and resolves
/// collision-free file names inside it.
/// </summary>
public class TournamentFolderTests
{
    [Fact]
    public void ResolveRoot_returns_default_when_settings_blank()
    {
        var root = TournamentFolder.ResolveRoot(new AppSettings { TournamentsRootFolder = null });
        Assert.Equal(TournamentFolder.DefaultRoot, root);
    }

    [Fact]
    public void ResolveRoot_returns_configured_value_when_set()
    {
        var root = TournamentFolder.ResolveRoot(new AppSettings { TournamentsRootFolder = @"C:\Custom\Events" });
        Assert.Equal(@"C:\Custom\Events", root);
    }

    [Fact]
    public void SanitizeForPathSegment_replaces_invalid_chars()
    {
        Assert.Equal("US Open 2024", TournamentFolder.SanitizeForPathSegment("US Open 2024"));
        // Pipe is invalid on Windows; should be replaced.
        var sanitized = TournamentFolder.SanitizeForPathSegment("US | Open");
        Assert.DoesNotContain("|", sanitized);
    }

    [Fact]
    public void SanitizeForPathSegment_blank_falls_back_to_Untitled()
    {
        Assert.Equal("Untitled", TournamentFolder.SanitizeForPathSegment(""));
        Assert.Equal("Untitled", TournamentFolder.SanitizeForPathSegment("   "));
        Assert.Equal("Untitled", TournamentFolder.SanitizeForPathSegment(null));
    }

    [Fact]
    public void ResolveEventFolder_combines_root_and_sanitized_name()
    {
        var folder = TournamentFolder.ResolveEventFolder(@"C:\Events", "Spring Open 2026");
        Assert.Equal(@"C:\Events\Spring Open 2026", folder);
    }

    [Fact]
    public void EnsureEventFolder_creates_missing_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fp-tf-{System.Guid.NewGuid():N}");
        try
        {
            var folder = TournamentFolder.EnsureEventFolder(root, "My Event");
            Assert.True(Directory.Exists(folder));
            Assert.EndsWith("My Event", folder);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveUniqueFilePath_appends_numeric_suffix_on_collision()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"fp-tf-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            // First call — folder empty, returns "event.sjson".
            var first = TournamentFolder.ResolveUniqueFilePath(folder, "event", ".sjson");
            Assert.Equal(Path.Combine(folder, "event.sjson"), first);

            File.WriteAllText(first, "stub");
            var second = TournamentFolder.ResolveUniqueFilePath(folder, "event", ".sjson");
            Assert.Equal(Path.Combine(folder, "event(1).sjson"), second);

            File.WriteAllText(second, "stub");
            var third = TournamentFolder.ResolveUniqueFilePath(folder, "event", ".sjson");
            Assert.Equal(Path.Combine(folder, "event(2).sjson"), third);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void ResolveUniqueFilePath_tolerates_extension_without_leading_dot()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"fp-tf-{System.Guid.NewGuid():N}");
        try
        {
            var path = TournamentFolder.ResolveUniqueFilePath(folder, "x", "sjson");
            Assert.Equal(Path.Combine(folder, "x.sjson"), path);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }
}
