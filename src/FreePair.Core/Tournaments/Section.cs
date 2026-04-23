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
    IReadOnlyList<(int A, int B)>? DoNotPairPairs = null)
{
    /// <summary>
    /// Non-null view of <see cref="DoNotPairPairs"/>.
    /// </summary>
    public IReadOnlyList<(int A, int B)> DoNotPairs =>
        DoNotPairPairs ?? System.Array.Empty<(int, int)>();
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
            if (r.Kind == RoundResultKind.None && r.Opponent == 0)
            {
                return true;
            }
        }

        return false;
    }
}
