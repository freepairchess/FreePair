using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Bbp;

/// <summary>
/// Invokes an external Swiss pairing engine (BBP / bbpPairings) to generate
/// the next round's pairings for a single section.
/// </summary>
public interface IBbpPairingEngine
{
    /// <summary>
    /// Runs the engine against a TRF rendering of the given
    /// <paramref name="section"/> and returns the next round's pairings.
    /// </summary>
    /// <param name="initialColor">
    /// Colour the top seed receives on board 1 of round 1. Ignored once
    /// the section already has played rounds. Defaults to
    /// <see cref="InitialColor.White"/>.
    /// </param>
    /// <exception cref="BbpNotConfiguredException">
    /// The <paramref name="executablePath"/> is null/empty or does not refer
    /// to an existing file.
    /// </exception>
    /// <exception cref="BbpExecutionException">
    /// The engine exited with a non-zero status; the message contains its
    /// diagnostic output.
    /// </exception>
    Task<BbpPairingResult> GenerateNextRoundAsync(
        string? executablePath,
        Tournament tournament,
        Section section,
        InitialColor initialColor = InitialColor.White,
        CancellationToken cancellationToken = default);
}
