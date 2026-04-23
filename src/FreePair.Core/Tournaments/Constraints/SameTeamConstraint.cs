using System;

namespace FreePair.Core.Tournaments.Constraints;

/// <summary>
/// Flags pairings between two players who share the same (case-insensitive,
/// non-empty) <see cref="Player.Team"/>. Scholastic and club tournaments
/// typically enable this so teammates don't meet until late in the
/// event.
/// </summary>
public sealed class SameTeamConstraint : IPairingConstraint
{
    public bool Violates(Player a, Player b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (string.IsNullOrWhiteSpace(a.Team)) return false;
        return string.Equals(a.Team, b.Team, StringComparison.OrdinalIgnoreCase);
    }

    public string Describe(Player a, Player b) => $"same team '{a.Team}'";
}
