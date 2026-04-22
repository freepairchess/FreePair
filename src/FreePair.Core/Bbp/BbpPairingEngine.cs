using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.Tournaments;
using FreePair.Core.Trf;

namespace FreePair.Core.Bbp;

/// <summary>
/// Default <see cref="IBbpPairingEngine"/> that drives <c>bbpPairings</c>
/// (https://github.com/BieremaBoyzProgramming/bbpPairings) via a subprocess:
/// it writes a TRF to a temp file, runs the engine with
/// <c>--dutch -p &lt;pairings.txt&gt;</c>, and parses the plain-text output.
/// </summary>
public class BbpPairingEngine : IBbpPairingEngine
{
    /// <summary>
    /// Friendly instructions shown when the user has not yet configured the
    /// pairing engine binary path. Includes the official releases URL.
    /// </summary>
    public const string NotConfiguredInstructions =
        "Pairing engine (BBP) is not configured.\n\n" +
        "1. Download bbpPairings from:\n" +
        "   https://github.com/BieremaBoyzProgramming/bbpPairings/releases\n" +
        "2. Unzip the release somewhere on your computer (e.g. C:\\Tools\\bbpPairings).\n" +
        "3. Open Settings in FreePair and set \"Pairing engine binary\" to the\n" +
        "   bbppairings executable inside that folder.";

    public async Task<BbpPairingResult> GenerateNextRoundAsync(
        string? executablePath,
        Tournament tournament,
        Section section,
        InitialColor initialColor = InitialColor.White,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(section);

        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            throw new BbpNotConfiguredException(NotConfiguredInstructions);
        }

        var trfPath = Path.Combine(Path.GetTempPath(),
            $"freepair-{Guid.NewGuid():N}.trf");
        var pairingsPath = Path.Combine(Path.GetTempPath(),
            $"freepair-{Guid.NewGuid():N}.pairings.txt");

        try
        {
            await using (var writer = new StreamWriter(trfPath, append: false, Encoding.ASCII))
            {
                TrfWriter.Write(tournament, section, writer, initialColor);
            }

            var psi = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("--dutch");
            psi.ArgumentList.Add(trfPath);
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add(pairingsPath);

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken)
                .ConfigureAwait(false);
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken)
                .ConfigureAwait(false);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var diagnostic = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;

                // Keep the TRF around for the user to inspect when BBP
                // rejects it — nothing worse than a parser error with no
                // way to see what the parser read.
                string? preservedTrf = null;
                try
                {
                    preservedTrf = Path.Combine(Path.GetTempPath(),
                        $"freepair-failed-{DateTime.UtcNow:yyyyMMddHHmmss}.trf");
                    File.Copy(trfPath, preservedTrf, overwrite: true);
                }
                catch { /* best effort */ }

                var tail = preservedTrf is null
                    ? string.Empty
                    : $"\n\n(The TRF that bbpPairings rejected has been preserved at: {preservedTrf})";

                throw new BbpExecutionException(
                    process.ExitCode,
                    $"Pairing engine exited with code {process.ExitCode}.\n{diagnostic}{tail}".TrimEnd());
            }

            if (!File.Exists(pairingsPath))
            {
                throw new BbpExecutionException(
                    process.ExitCode,
                    "Pairing engine did not produce an output file.");
            }

            var text = await File.ReadAllTextAsync(pairingsPath, cancellationToken)
                .ConfigureAwait(false);
            return BbpPairingsParser.Parse(text);
        }
        finally
        {
            TryDelete(trfPath);
            TryDelete(pairingsPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort — don't mask the real exception.
        }
    }
}
