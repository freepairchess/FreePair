namespace FreePair.Core.Updates;

/// <summary>
/// In-app upgrade story: poll the published GitHub Releases feed,
/// surface available updates in the UI, and apply the update on TD
/// confirmation. Backed by Velopack's <c>UpdateManager</c>; the
/// implementation lives in the App project so Core stays free of
/// the Velopack reference (which only matters for the installed
/// desktop build).
/// <para>
/// Returns <see cref="UpdateCheckResult"/> from <see cref="CheckAsync"/>:
/// either <c>UpToDate</c>, <c>Available(version, notesMarkdown)</c>,
/// or <c>Failed(message)</c>. The UI binds to those three cases
/// without taking a Velopack dependency.
/// </para>
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Polls the configured releases feed for a newer version.
    /// Safe to call repeatedly; the underlying manager dedupes
    /// network calls when invoked rapid-fire from the UI.
    /// </summary>
    System.Threading.Tasks.Task<UpdateCheckResult> CheckAsync(
        System.Threading.CancellationToken ct = default);

    /// <summary>
    /// Downloads and applies the most recent available update,
    /// then exits FreePair so Velopack's launcher can swap in the
    /// new version. The TD's tournament file is auto-saved before
    /// this call returns. Throws if no update is available;
    /// callers should gate this on a successful
    /// <see cref="UpdateCheckResult.Available"/> from
    /// <see cref="CheckAsync"/>.
    /// </summary>
    System.Threading.Tasks.Task ApplyAndRestartAsync(
        System.Threading.CancellationToken ct = default);
}

/// <summary>Outcome of <see cref="IUpdateService.CheckAsync"/>.</summary>
public abstract record UpdateCheckResult
{
    /// <summary>FreePair is on the latest published release.</summary>
    public sealed record UpToDate : UpdateCheckResult;

    /// <summary>
    /// A newer release is available. <see cref="Version"/> is the
    /// SemVer string (no leading 'v'); <see cref="ReleaseNotes"/>
    /// is the GitHub release body in Markdown (may be empty).
    /// </summary>
    public sealed record Available(string Version, string ReleaseNotes) : UpdateCheckResult;

    /// <summary>
    /// The check failed (no network, GitHub rate-limit, repo
    /// misconfigured, dev build with no install metadata). The
    /// message is suitable for surfacing to TDs.
    /// </summary>
    public sealed record Failed(string Message) : UpdateCheckResult;

    /// <summary>
    /// FreePair is running outside an installed Velopack package
    /// (e.g. dev / dotnet run / portable extract). Update checks
    /// are intentionally skipped in this case so we don't trigger
    /// false positives on pre-release dev branches.
    /// </summary>
    public sealed record NotInstalled : UpdateCheckResult;
}
