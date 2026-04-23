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
    /// the section already has played rounds. When <c>null</c>, the
    /// engine falls back to <see cref="Section.InitialColor"/> (which
    /// is populated from SwissSys's per-section <c>Coin toss</c>
    /// field on load).
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
        InitialColor? initialColor = null,
        CancellationToken cancellationToken = default);
}
