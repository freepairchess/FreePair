using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        // The round BBP is about to pair — one past the completed ones.
        // Used to pre-flag requested-bye players in the TRF so BBP honours
        // them instead of pairing them.
        var pairingRound = section.RoundsPlayed + 1;

        // Pair numbers whose RequestedByeRounds contain the upcoming
        // round. We pass this back in the result so AppendRound can
        // stamp a HalfPointBye history entry for them.
        var requestedHalfByes = section.Players
            .Where(p => p.RequestedByeRounds.Contains(pairingRound))
            .Select(p => p.PairNumber)
            .ToArray();

        try
        {
            await using (var writer = new StreamWriter(trfPath, append: false, Encoding.ASCII))
            {
                TrfWriter.Write(tournament, section, writer, initialColor, pairingRound);
            }

            var psi = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var arg in BuildArguments(section, trfPath, pairingsPath))
            {
                psi.ArgumentList.Add(arg);
            }

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
            var parsed = BbpPairingsParser.Parse(text);

            // Merge the requested-bye pair numbers into the result so
            // the caller (TournamentMutations.AppendRound) records the
            // HalfPointBye history entry for those players.
            return new BbpPairingResult(
                parsed.Pairings,
                parsed.ByePlayerPairs,
                HalfPointByePlayerPairs: requestedHalfByes);
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

    /// <summary>
    /// Builds the bbpPairings command-line arguments for
    /// <paramref name="section"/>. Exposed as <c>internal</c> so tests
    /// can assert flag decisions without spawning a subprocess.
    /// </summary>
    /// <remarks>
    /// Emits <c>--dutch</c> by default. When
    /// <see cref="Tournaments.Section.UseAcceleration"/> is true, also
    /// emits <c>--baku</c> — bbpPairings' FIDE Baku-style
    /// acceleration, the same technique SwissSys applies for its
    /// "Acceleration" section setting. Accelerated pairings give the
    /// top half a virtual score bump in early rounds so a large field
    /// splits correctly in a short Swiss.
    /// </remarks>
    internal static IReadOnlyList<string> BuildArguments(
        Tournaments.Section section,
        string trfPath,
        string pairingsPath)
    {
        var args = new List<string> { "--dutch" };
        if (section.UseAcceleration)
        {
            args.Add("--baku");
        }
        args.Add(trfPath);
        args.Add("-p");
        args.Add(pairingsPath);
        return args;
    }
}
