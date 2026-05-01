namespace FreePair.Core.Tournaments;

/// <summary>
/// A played (or scheduled) board pairing between two players.
/// <para>
/// <see cref="Note"/> is a session-only TD annotation — set when a
/// forced swap deliberately recreates a previously-played game, or
/// any other manual override that the TD wants to flag for paper-
/// pairing review. Round-trip through the SwissSys writer is
/// intentionally NOT supported: notes apply to the current sitting
/// only and are gone once the game is played and results entered.
/// </para>
/// </summary>
public sealed record Pairing(
    int Board,
    int WhitePair,
    int BlackPair,
    PairingResult Result,
    string? Note = null);
