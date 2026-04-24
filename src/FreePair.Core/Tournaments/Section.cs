using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments;

/// <summary>
/// A single tournament section (e.g. "Open I", "Under_1000").
/// </summary>
public sealed record Section(
    string Name,
    string? Title,
    SectionKind Kind,
    string? TimeControl,
    int RoundsPaired,
    int RoundsPlayed,
    int FinalRound,
    int? FirstBoard,
    IReadOnlyList<Player> Players,
    IReadOnlyList<Team> Teams,
    IReadOnlyList<Round> Rounds,
    Prizes Prizes,
    bool UseAcceleration = false,
    InitialColor InitialColor = InitialColor.White,
    bool AvoidSameTeam = true,
    bool AvoidSameClub = false,
    IReadOnlyList<(int A, int B)>? DoNotPairPairs = null,
    IReadOnlyList<ForcedPairing>? ForcedPairings = null,
    /// <summary>
    /// When <c>true</c>, the section is soft-deleted. FreePair blocks
    /// mutations against it and the publishing pipeline excludes it
    /// from the results JSON. Toggled via
    /// <see cref="TournamentMutations.SoftDeleteSection"/> /
    /// <see cref="TournamentMutations.UndeleteSection"/>; persisted
    /// as <c>"FreePair soft deleted"</c> in the section's Overview.
    /// </summary>
    bool SoftDeleted = false)
{
    /// <summary>
    /// Non-null view of <see cref="DoNotPairPairs"/>.
    /// </summary>
    public IReadOnlyList<(int A, int B)> DoNotPairs =>
        DoNotPairPairs ?? System.Array.Empty<(int, int)>();

    /// <summary>
    /// Non-null view of <see cref="ForcedPairings"/>.
    /// </summary>
    public IReadOnlyList<ForcedPairing> ForcedPairs =>
        ForcedPairings ?? System.Array.Empty<ForcedPairing>();
    /// <summary>True when this section tracks teams in addition to individuals.</summary>
    public bool HasTeams => Teams.Count > 0;

    /// <summary>
    /// Players whose round history contains an unpaired slot (<c>~</c>) in
    /// any already-played round — typically withdrawals or late additions.
    /// </summary>
    public IEnumerable<Player> WithdrawnPlayers => Players.Where(IsWithdrawn);

    /// <summary>
    /// True when the player has at least one unpaired round within the range
    /// of rounds that have already been played.
    /// </summary>
    public bool IsWithdrawn(Player player)
    {
        if (player is null)
        {
            return false;
        }

        var playedCount = System.Math.Min(RoundsPlayed, player.History.Count);
        for (var i = 0; i < playedCount; i++)
        {
            var r = player.History[i];
            // SwissSys marks withdrawal rounds with kind "~" / None /
            // "U" (ZeroPointBye) and no opponent. Both parse variants
            // count as a withdrawn-round marker.
            if ((r.Kind == RoundResultKind.None || r.Kind == RoundResultKind.ZeroPointBye)
                && r.Opponent == 0)
            {
                return true;
            }
        }

        return false;
    }
}
