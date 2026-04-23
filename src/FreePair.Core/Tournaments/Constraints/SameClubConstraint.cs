using System;

namespace FreePair.Core.Tournaments.Constraints;

/// <summary>
/// Flags pairings between two players who share the same (case-insensitive,
/// non-empty) <see cref="Player.Club"/>. Useful for local-league or
/// scholastic events where the organiser wants to minimise intra-club
/// games.
/// </summary>
public sealed class SameClubConstraint : IPairingConstraint
{
    public bool Violates(Player a, Player b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (string.IsNullOrWhiteSpace(a.Club)) return false;
        return string.Equals(a.Club, b.Club, StringComparison.OrdinalIgnoreCase);
    }

    public string Describe(Player a, Player b) => $"same club '{a.Club}'";
}
