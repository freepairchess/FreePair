using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Bbp;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Constraints;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Correctness tests for <see cref="PairingSwapper"/> and the
/// constraint implementations. Builds small synthetic sections whose
/// scores we fully control so the same-score-group swap rule is easy
/// to reason about.
/// </summary>
public class PairingSwapperTests
{
    private static Player P(
        int pair,
        string name,
        decimal score,
        string? team = null,
        string? club = null,
        int[]? pastOpponents = null)
    {
        // Stamp past-opponent history as a list of Draw entries so the
        // derived Score matches the target; pad with None entries to
        // make scores exactly match requested values.
        var history = new List<FreePair.Core.SwissSys.RoundResult>();
        if (pastOpponents is not null)
        {
            foreach (var opp in pastOpponents)
            {
                history.Add(new FreePair.Core.SwissSys.RoundResult(
                    Kind: FreePair.Core.SwissSys.RoundResultKind.Draw,
                    Opponent: opp,
                    Color: FreePair.Core.SwissSys.PlayerColor.White,
                    Board: 1, Logic1: 0, Logic2: 0, GamePoints: 0m));
            }
        }

        // If needed, top-up the score with a synthetic draw.
        var existingScore = history.Sum(h => h.Score);
        if (score > existingScore)
        {
            var gap = score - existingScore;
            while (gap >= 1m)
            {
                history.Add(new FreePair.Core.SwissSys.RoundResult(
                    FreePair.Core.SwissSys.RoundResultKind.FullPointBye,
                    -1, FreePair.Core.SwissSys.PlayerColor.None, 0, 0, 0, 0m));
                gap -= 1m;
            }
            if (gap >= 0.5m)
            {
                history.Add(new FreePair.Core.SwissSys.RoundResult(
                    FreePair.Core.SwissSys.RoundResultKind.HalfPointBye,
                    -1, FreePair.Core.SwissSys.PlayerColor.None, 0, 0, 0, 0m));
            }
        }

        return new Player(
            PairNumber: pair,
            Name: name,
            UscfId: null,
            Rating: 1500,
            SecondaryRating: null,
            MembershipExpiration: null,
            Club: club,
            State: null,
            Team: team,
            RequestedByeRounds: Array.Empty<int>(),
            History: history);
    }

    private static Section BuildSection(params Player[] players) => new(
        Name: "Test",
        Title: null,
        Kind: SectionKind.Swiss,
        TimeControl: null,
        RoundsPaired: 0,
        RoundsPlayed: 0,
        FinalRound: 5,
        FirstBoard: 1,
        Players: players,
        Teams: Array.Empty<Team>(),
        Rounds: Array.Empty<Round>(),
        Prizes: new Prizes(Array.Empty<Prize>(), Array.Empty<Prize>()));

    // ============ constraints ============

    [Fact]
    public void SameTeamConstraint_flags_only_matching_non_empty_team()
    {
        var c = new SameTeamConstraint();
        var daasA = P(1, "Adrito", 1m, team: "Daas");
        var daasB = P(2, "Aritra", 1m, team: "Daas");
        var other = P(3, "Other",  1m, team: "Other");
        var noTeam = P(4, "None",  1m, team: null);

        Assert.True (c.Violates(daasA, daasB));
        Assert.True (c.Violates(daasB, daasA)); // order-insensitive
        Assert.False(c.Violates(daasA, other));
        Assert.False(c.Violates(daasA, noTeam));
        Assert.False(c.Violates(noTeam, noTeam));
    }

    [Fact]
    public void SameClubConstraint_flags_only_matching_non_empty_club_case_insensitive()
    {
        var c = new SameClubConstraint();
        var a = P(1, "A", 1m, club: "Mechanics");
        var b = P(2, "B", 1m, club: "mechanics"); // casing differs
        var d = P(3, "D", 1m, club: "Other");
        var e = P(4, "E", 1m, club: null);

        Assert.True (c.Violates(a, b));
        Assert.False(c.Violates(a, d));
        Assert.False(c.Violates(a, e));
    }

    [Fact]
    public void DoNotPairConstraint_is_order_insensitive_and_dedupes()
    {
        var c = new DoNotPairConstraint(new[] { (3, 7), (7, 3), (3, 7), (5, 5) });
        var p3 = P(3, "X", 1m);
        var p7 = P(7, "Y", 1m);
        var p5 = P(5, "Z", 1m);

        Assert.True (c.Violates(p3, p7));
        Assert.True (c.Violates(p7, p3));
        Assert.False(c.Violates(p3, p5));
        Assert.False(c.Violates(p5, p5)); // self-pairs dropped
    }

    // ============ swapper ============

    [Fact]
    public void Apply_is_noop_when_constraint_list_is_empty()
    {
        var section = BuildSection(P(1, "A", 1m), P(2, "B", 1m));
        var pairings = new[] { new BbpPairing(1, 2) };

        var result = PairingSwapper.Apply(pairings, section, Array.Empty<IPairingConstraint>());

        Assert.Same(pairings, result.Pairings);
        Assert.Empty(result.UnresolvedConflicts);
    }

    [Fact]
    public void Apply_is_noop_when_no_pairing_violates_any_constraint()
    {
        var section = BuildSection(
            P(1, "A", 1m, team: "TeamA"),
            P(2, "B", 1m, team: "TeamB"));
        var pairings = new[] { new BbpPairing(1, 2) };

        var result = PairingSwapper.Apply(
            pairings, section, new[] { new SameTeamConstraint() });

        Assert.Equal(pairings, result.Pairings);
        Assert.Empty(result.UnresolvedConflicts);
    }

    [Fact]
    public void Apply_swaps_two_same_team_pairings_when_cross_swap_resolves_both()
    {
        //  Pair 1 (TeamX) vs Pair 2 (TeamX)  — violates SameTeam
        //  Pair 3 (TeamY) vs Pair 4 (TeamY)  — violates SameTeam
        //
        //  Swap yields (1 vs 4), (3 vs 2)   — both constraint-free.
        var section = BuildSection(
            P(1, "A", 1m, team: "TeamX"),
            P(2, "B", 1m, team: "TeamX"),
            P(3, "C", 1m, team: "TeamY"),
            P(4, "D", 1m, team: "TeamY"));

        var pairings = new[]
        {
            new BbpPairing(1, 2),
            new BbpPairing(3, 4),
        };

        var result = PairingSwapper.Apply(
            pairings, section, new[] { new SameTeamConstraint() });

        Assert.Empty(result.UnresolvedConflicts);
        // After swap, pair 1 is no longer against pair 2 nor pair 3 (same team).
        var pair1 = result.Pairings.Single(p => p.WhitePair == 1 || p.BlackPair == 1);
        var opp = pair1.WhitePair == 1 ? pair1.BlackPair : pair1.WhitePair;
        Assert.Equal(4, opp);
    }

    [Fact]
    public void Apply_preserves_colour_stability_in_the_swap()
    {
        // (1w vs 2b), (3w vs 4b) both violate. After swap:
        //   1 should keep white (paired with 4 now, who keeps black),
        //   3 should keep white (paired with 2 now, who keeps black).
        var section = BuildSection(
            P(1, "A", 1m, team: "TeamX"),
            P(2, "B", 1m, team: "TeamX"),
            P(3, "C", 1m, team: "TeamY"),
            P(4, "D", 1m, team: "TeamY"));
        var pairings = new[] { new BbpPairing(1, 2), new BbpPairing(3, 4) };

        var result = PairingSwapper.Apply(
            pairings, section, new[] { new SameTeamConstraint() });

        Assert.Contains(result.Pairings, p => p.WhitePair == 1 && p.BlackPair == 4);
        Assert.Contains(result.Pairings, p => p.WhitePair == 3 && p.BlackPair == 2);
    }

    [Fact]
    public void Apply_refuses_cross_score_group_swap()
    {
        // (1 vs 2) violates SameTeam. Only alternative (3 vs 4) is in a
        // DIFFERENT score group, so swap is rejected and the conflict
        // is reported as unresolvable.
        var section = BuildSection(
            P(1, "A", 2m, team: "TeamX"),
            P(2, "B", 2m, team: "TeamX"),
            P(3, "C", 0m, team: "TeamY"),
            P(4, "D", 0m, team: "TeamY"));
        var pairings = new[] { new BbpPairing(1, 2), new BbpPairing(3, 4) };

        var result = PairingSwapper.Apply(
            pairings, section, new[] { new SameTeamConstraint() });

        // Both conflicts remain; pairings unchanged.
        Assert.Equal(pairings, result.Pairings);
        Assert.Equal(2, result.UnresolvedConflicts.Count);
    }

    [Fact]
    public void Apply_refuses_swap_that_would_recreate_a_previously_played_game()
    {
        // Pair 1 already played pair 4 last round. Swap would pair them
        // again; must be rejected even though it resolves the team conflict.
        var section = BuildSection(
            P(1, "A", 1m, team: "TeamX", pastOpponents: new[] { 4 }),
            P(2, "B", 1m, team: "TeamX"),
            P(3, "C", 1m, team: "TeamY"),
            P(4, "D", 1m, team: "TeamY"));
        var pairings = new[] { new BbpPairing(1, 2), new BbpPairing(3, 4) };

        var result = PairingSwapper.Apply(
            pairings, section, new[] { new SameTeamConstraint() });

        // Swap rejected → conflicts remain.
        Assert.Equal(pairings, result.Pairings);
        Assert.NotEmpty(result.UnresolvedConflicts);
    }

    [Fact]
    public void Apply_honours_do_not_pair_constraint()
    {
        // TD has blacklisted (1, 2). BBP paired them anyway; swapper
        // must rearrange against another pairing (3 vs 4) in the same
        // score group.
        var section = BuildSection(
            P(1, "A", 1m),
            P(2, "B", 1m),
            P(3, "C", 1m),
            P(4, "D", 1m));
        var pairings = new[] { new BbpPairing(1, 2), new BbpPairing(3, 4) };

        var dnp = new DoNotPairConstraint(new[] { (1, 2) });
        var result = PairingSwapper.Apply(pairings, section, new IPairingConstraint[] { dnp });

        Assert.Empty(result.UnresolvedConflicts);
        Assert.DoesNotContain(
            result.Pairings, p => (p.WhitePair == 1 && p.BlackPair == 2)
                               || (p.WhitePair == 2 && p.BlackPair == 1));
    }
}
