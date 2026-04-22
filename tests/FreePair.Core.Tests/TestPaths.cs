using System;
using System.IO;

namespace FreePair.Core.Tests;

/// <summary>
/// Helpers for locating repo-relative fixture files from within a unit test
/// without relying on MSBuild <c>CopyToOutputDirectory</c> (which is
/// unreliable in some build environments).
/// </summary>
internal static class TestPaths
{
    private static readonly Lazy<string> s_repoRoot = new(FindRepoRoot);

    /// <summary>
    /// Absolute path to the repository root (the directory that contains the
    /// <c>FreePair.sln</c> file).
    /// </summary>
    public static string RepoRoot => s_repoRoot.Value;

    /// <summary>
    /// Absolute path to a file inside <c>docs/samples/swisssys</c>.
    /// </summary>
    public static string SwissSysSample(string fileName) =>
        Path.Combine(RepoRoot, "docs", "samples", "swisssys", fileName);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FreePair.slnx")) ||
                File.Exists(Path.Combine(directory.FullName, "FreePair.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate FreePair solution walking up from '{AppContext.BaseDirectory}'.");
    }
}
