using System;
using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.Updates;
using Velopack;
using Velopack.Sources;

namespace FreePair.App.Services;

/// <summary>
/// Production <see cref="IUpdateService"/> backed by Velopack's
/// <see cref="UpdateManager"/> against the FreePair GitHub Releases
/// feed. Lives in the App project so the Core library stays free
/// of the Velopack package reference (which only matters for the
/// installed Windows desktop build).
/// <para>
/// The repo URL and "include pre-releases" flag are passed in by
/// the host (typically <see cref="MainWindowViewModel"/> reading
/// from <see cref="AppSettings"/>) so we don't hard-code
/// <c>freepairchess/FreePair</c> in Core code: testing and forks
/// override via configuration.
/// </para>
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    private readonly string _githubRepoUrl;
    private readonly bool _includePreReleases;
    private readonly UpdateManager? _manager;

    /// <param name="githubRepoUrl">
    /// Full URL of the GitHub repo hosting releases, e.g.
    /// <c>https://github.com/freepairchess/FreePair</c>. NOT a
    /// raw <c>owner/repo</c> string.
    /// </param>
    /// <param name="includePreReleases">
    /// When <c>true</c>, the <see cref="GithubSource"/> considers
    /// pre-release tags. Off by default so TDs on the stable
    /// channel don't get pulled into beta builds.
    /// </param>
    public VelopackUpdateService(string githubRepoUrl, bool includePreReleases = false)
    {
        _githubRepoUrl = githubRepoUrl ?? throw new ArgumentNullException(nameof(githubRepoUrl));
        _includePreReleases = includePreReleases;

        // UpdateManager throws on construction when the host isn't
        // running from an installed Velopack package (i.e. dev /
        // dotnet run / portable extract). Catch + null-out so
        // CheckAsync can return NotInstalled cleanly instead of
        // pushing a try/catch onto every UI caller.
        try
        {
            var source = new GithubSource(_githubRepoUrl, accessToken: null,
                prerelease: _includePreReleases, downloader: null);
            _manager = new UpdateManager(source, options: null, locator: null);
        }
        catch
        {
            _manager = null;
        }
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        if (_manager is null) return new UpdateCheckResult.NotInstalled();
        try
        {
            var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info is null) return new UpdateCheckResult.UpToDate();
            var version = info.TargetFullRelease?.Version?.ToString() ?? "unknown";
            var notes   = info.TargetFullRelease?.NotesMarkdown ?? string.Empty;
            return new UpdateCheckResult.Available(version, notes);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task ApplyAndRestartAsync(CancellationToken ct = default)
    {
        if (_manager is null)
            throw new InvalidOperationException("FreePair is not running from an installed package; cannot apply update.");

        var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("No update available to apply.");

        await _manager.DownloadUpdatesAsync(info).ConfigureAwait(false);
        // ApplyUpdatesAndRestart never returns: it spawns the
        // updater and exits the current process. Anything below
        // this line will not run.
        _manager.ApplyUpdatesAndRestart(info);
    }
}
