using System;
using System.IO;
using System.Text;
using FreePair.Core.Settings;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Helpers for locating tournament files on disk. FreePair's default
/// layout is one folder per event, rooted at
/// <see cref="AppSettings.TournamentsRootFolder"/> (or the built-in
/// default <see cref="DefaultRoot"/>). Each folder holds the
/// <c>.sjson</c> plus any per-event PDFs, backups, etc.
/// </summary>
public static class TournamentFolder
{
    /// <summary>
    /// Built-in default — <c>Documents/FreePairEvents</c> for the
    /// current user. Used when <see cref="AppSettings.TournamentsRootFolder"/>
    /// is not set.
    /// </summary>
    public static string DefaultRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "FreePairEvents");

    /// <summary>
    /// Resolves the effective root folder for the TD's tournaments.
    /// </summary>
    public static string ResolveRoot(AppSettings? settings)
    {
        var configured = settings?.TournamentsRootFolder;
        return string.IsNullOrWhiteSpace(configured)
            ? DefaultRoot
            : configured!.Trim();
    }

    /// <summary>
    /// Returns <c>{root}/{sanitized event name}</c>. Does not create
    /// the folder — caller decides whether to mkdir (e.g. via
    /// <see cref="EnsureEventFolder"/>) or just use the path for a
    /// save-file picker's SuggestedStartLocation.
    /// </summary>
    public static string ResolveEventFolder(string root, string eventName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var safe = SanitizeForPathSegment(eventName);
        return Path.Combine(root, safe);
    }

    /// <summary>
    /// Ensures the per-event folder exists (creating it and any
    /// missing parents) and returns its absolute path.
    /// </summary>
    public static string EnsureEventFolder(string root, string eventName)
    {
        var path = ResolveEventFolder(root, eventName);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Returns a path inside <paramref name="folder"/> whose file name
    /// does not yet exist on disk, using the Windows-familiar
    /// <c>name(1).ext</c> / <c>name(2).ext</c> suffix pattern for
    /// collisions. Creates the folder if needed.
    /// </summary>
    /// <param name="folder">Target directory. Created if missing.</param>
    /// <param name="baseName">File name without extension or collision suffix.</param>
    /// <param name="extension">Extension including the leading dot, e.g. <c>".sjson"</c>.</param>
    public static string ResolveUniqueFilePath(string folder, string baseName, string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        if (!extension.StartsWith('.')) extension = "." + extension;

        Directory.CreateDirectory(folder);

        var safeBase = SanitizeForFileName(baseName);
        var candidate = Path.Combine(folder, safeBase + extension);
        if (!File.Exists(candidate)) return candidate;

        for (var i = 1; i < 10_000; i++)
        {
            candidate = Path.Combine(folder, $"{safeBase}({i}){extension}");
            if (!File.Exists(candidate)) return candidate;
        }
        throw new IOException(
            $"Unable to find an unused file name under '{folder}' " +
            $"after 10,000 attempts with base '{safeBase}{extension}'.");
    }

    // ==== sanitization ===================================================

    /// <summary>
    /// Replaces every filesystem-invalid char with <c>_</c> and trims
    /// leading/trailing separators so the returned string is safe to
    /// use as a single path segment on every platform we support.
    /// Falls back to <c>"Untitled"</c> when the input sanitizes down
    /// to nothing.
    /// </summary>
    public static string SanitizeForPathSegment(string? raw)
    {
        var reserved = Path.GetInvalidPathChars();
        var fnInvalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var c in (raw ?? string.Empty).Trim())
        {
            sb.Append(Array.IndexOf(fnInvalid, c) >= 0 || Array.IndexOf(reserved, c) >= 0
                ? '_' : c);
        }
        var result = sb.ToString().Trim('_', ' ', '.');
        return string.IsNullOrWhiteSpace(result) ? "Untitled" : result;
    }

    /// <summary>
    /// Same as <see cref="SanitizeForPathSegment"/> but also strips
    /// any character that's invalid in a file-name (which is a superset
    /// of path-invalid on Windows — <c>*</c>, <c>?</c>, etc.).
    /// </summary>
    public static string SanitizeForFileName(string? raw) =>
        SanitizeForPathSegment(raw);
}
