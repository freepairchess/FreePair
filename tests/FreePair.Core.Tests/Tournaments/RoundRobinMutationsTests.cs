using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// End-to-end tests for the round-robin pair-next-round flow: build a
/// synthetic RR section from scratch, call
/// <see cref="TournamentMutations.AppendRoundRobinRound"/> for every
/// scheduled round, and verify that the resulting history is complete
/// and consistent with <see cref="RoundRobinScheduler"/>.
/// </summary>
public class RoundRobinMutationsTests
{
    private static Tournament BuildRoundRobin(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => new Player(
                PairNumber: i,
                Name: $"Player{i}",
                UscfId: null,
                Rating: 1500 - i,
                SecondaryRating: null,
                MembershipExpiration: null,
                Club: null,
                State: null,
                Team: null,
                RequestedByeRounds: Array.Empty<int>(),
                History: Array.Empty<RoundResult>()))
            .ToArray();

        var section = new Section(
            Name: "RR",
            Title: "Round Robin",
            Kind: SectionKind.RoundRobin,
            TimeControl: "G/45",
            RoundsPaired: 0,
            RoundsPlayed: 0,
            FinalRound: playerCount % 2 == 0 ? playerCount - 1 : playerCount,
            FirstBoard: 1,
            Players: players,
            Teams: Array.Empty<Team>(),
            Rounds: Array.Empty<Round>(),
            Prizes: new Prizes(Array.Empty<Prize>(), Array.Empty<Prize>()));

        return new Tournament(
            Title: "RR Test",
            StartDate: null,
            EndDate: null,
            TimeControl: "G/45",
            NachEventId: null,
            Sections: new[] { section });
    }

    [Fact]
    public void AppendRoundRobinRound_pairs_every_round_in_turn_for_even_field()
    {
        const int N = 6; // 5 rounds
        var t = BuildRoundRobin(N);

        for (var r = 0; r < N - 1; r++)
        {
            t = TournamentMutations.AppendRoundRobinRound(t, "RR");
        }

        var s = t.Sections.Single(x => x.Name == "RR");
        Assert.Equal(N - 1, s.Rounds.Count);
        Assert.Equal(N - 1, s.RoundsPaired);

        // Every player played exactly N-1 games, no byes.
        foreach (var p in s.Players)
        {
            Assert.Equal(N - 1, p.History.Count);
            Assert.DoesNotContain(
                s.Rounds.SelectMany(r => r.Byes),
                b => b.PlayerPair == p.PairNumber);
        }
    }

    [Fact]
    public void AppendRoundRobinRound_byes_one_player_per_round_for_odd_field()
    {
        const int N = 5; // 5 rounds, each player gets one full-point bye
        var t = BuildRoundRobin(N);

        for (var r = 0; r < N; r++)
        {
            t = TournamentMutations.AppendRoundRobinRound(t, "RR");
        }

        var s = t.Sections.Single(x => x.Name == "RR");
        Assert.Equal(N, s.Rounds.Count);

        // Every player byed exactly once, and their history records it
        // as FullPointBye.
        foreach (var p in s.Players)
        {
            var fullByes = p.History.Count(h => h.Kind == RoundResultKind.FullPointBye);
            Assert.Equal(1, fullByes);
            // Remaining entries are paired games (Opponent > 0).
            var paired = p.History.Count(h => h.Opponent > 0);
            Assert.Equal(N - 1, paired);
        }
    }

    [Fact]
    public void AppendRoundRobinRound_throws_after_schedule_exhausted()
    {
        var t = BuildRoundRobin(4);
        for (var r = 0; r < 3; r++)
        {
            t = TournamentMutations.AppendRoundRobinRound(t, "RR");
        }
        Assert.Throws<InvalidOperationException>(() =>
            TournamentMutations.AppendRoundRobinRound(t, "RR"));
    }

    [Fact]
    public void AppendRoundRobinRound_throws_for_non_RoundRobin_section()
    {
        var t = BuildRoundRobin(4) with { };
        var s = t.Sections[0] with { Kind = SectionKind.Swiss };
        t = t with { Sections = new[] { s } };

        Assert.Throws<InvalidOperationException>(() =>
            TournamentMutations.AppendRoundRobinRound(t, "RR"));
    }
}
