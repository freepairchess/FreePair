using System.Collections.Generic;

namespace FreePair.Core.Uscf.Trf;

/// <summary>
/// In-memory representation of a parsed FIDE TRF (Tournament Report File)
/// document — only the fields FreePair's USCF pairing engine cares about.
/// </summary>
/// <param name="TournamentName">Tag <c>012</c> value, may be empty.</param>
/// <param name="StartDate">ISO date from tag <c>042</c>, may be empty.</param>
/// <param name="EndDate">ISO date from tag <c>052</c>, may be empty.</param>
/// <param name="TotalRounds">
/// Total rounds from <c>XXR</c> when present; falls back to the
/// max <c>roundsPlayed + 1</c> across the player roster when absent.
/// </param>
/// <param name="InitialColor">
/// <c>'w'</c> when XXC is <c>white1</c>, <c>'b'</c> when <c>black1</c>,
/// <c>null</c> when no XXC directive was present. Drives round-1 colour
/// allocation.
/// </param>
/// <param name="Players">
/// One entry per <c>001</c> line. Order matches file order; pair numbers
/// are taken from the line itself, not from list position.
/// </param>
public sealed record TrfDocument(
    string TournamentName,
    string StartDate,
    string EndDate,
    int TotalRounds,
    char? InitialColor,
    IReadOnlyList<TrfPlayer> Players);
