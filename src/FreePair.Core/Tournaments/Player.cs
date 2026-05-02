using System.Collections.Generic;
using System.Linq;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments;

/// <summary>
/// A single player (or pair-number slot) in a tournament section.
/// </summary>
/// <param name="Withdrawn">
/// True when the TD has withdrawn this player from further pairing.
/// Withdrawn players are omitted from the TRF (and therefore from BBP's
/// pairing pool) and <see cref="TournamentMutations.AppendRound"/> does
/// not extend their history. Existing past results remain intact, so
/// standings / wall chart / tiebreaks continue to reflect games the
/// player did play. Session-only for v1: the flag is not round-tripped
/// through SwissSys <c>.sjson</c> save/load.
/// </param>
public sealed record Player(
    int PairNumber,
    string Name,
    string? UscfId,
    int Rating,
    int? SecondaryRating,
    string? MembershipExpiration,
    string? Club,
    string? State,
    string? Team,
    IReadOnlyList<int> RequestedByeRounds,
    IReadOnlyList<RoundResult> History,
    bool Withdrawn = false,
    string? Email = null,
    string? Phone = null,
    /// <summary>
    /// When <c>true</c>, the player is soft-deleted. Only permitted
    /// before any round of their section is paired
    /// (<see cref="Section.RoundsPaired"/> == 0); once paired, the
    /// mutations layer rejects the toggle and the TD must use
    /// <see cref="Withdrawn"/> instead. Soft-deleted players are
    /// excluded from standings, wall chart, TRF export, publishing,
    /// and BBP pairing input. Persisted as
    /// <c>"FreePair soft deleted"</c> in the raw player JSON.
    /// </summary>
    bool SoftDeleted = false,
    /// <summary>
    /// Round numbers where the TD has granted a zero-point bye to
    /// this player. Complements
    /// <see cref="RequestedByeRounds"/> (which is always half-point
    /// per SwissSys's standard "Reserved byes" field). Persisted as
    /// <c>"FreePair zero-point bye rounds"</c>. When the BBP engine
    /// pairs a round listed here, the player is excluded from
    /// pairing (filtered from the active roster) and their history
    /// is stamped <see cref="RoundResult.ZeroPointBye"/>.
    /// </summary>
    IReadOnlyList<int>? ZeroPointByeRounds = null,
    /// <summary>
    /// Chess title — typically <c>"GM"</c>, <c>"IM"</c>, <c>"FM"</c>,
    /// <c>"WGM"</c>, etc. for FIDE-titled players, or NM/SM/etc. for
    /// USCF national titles. Sourced from SwissSys's
    /// <c>"Player title"</c> field; <c>null</c> / blank for untitled
    /// players. Used by the Pairings tab to prefix the player's
    /// display name (e.g. "GM Sun, Ryan"). Round-trips via the raw
    /// JSON pass-through writer (FreePair doesn't write the field
    /// itself but preserves it from the source file).
    /// </summary>
    string? Title = null)
{
    /// <summary>
    /// Sum of scoring results across <see cref="History"/> (1 per win / full
    /// bye, 0.5 per draw / half bye, 0 otherwise).
    /// </summary>
    public decimal Score => History.Sum(r => r.Score);

    /// <summary>
    /// Non-null view of <see cref="ZeroPointByeRounds"/>. Always
    /// prefer this over the nullable field for consumers so empty
    /// collections don't need a null-check at every usage.
    /// </summary>
    public IReadOnlyList<int> ZeroPointByeRoundsOrEmpty =>
        ZeroPointByeRounds ?? System.Array.Empty<int>();
}
