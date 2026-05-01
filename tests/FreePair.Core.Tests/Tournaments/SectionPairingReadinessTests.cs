using System.Linq;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests <see cref="SectionPairingReadiness.Classify"/> across the
/// five buckets the Pair-all-sections dialog uses to decide which
/// sections to include in a batch pairing run.
/// </summary>
public class SectionPairingReadinessTests
{
    [Fact]
    public void Classifies_softdeleted_first()
    {
        var s = BuildSection(playerCount: 10, finalRound: 4);
        s = s with { SoftDeleted = true };
        Assert.Equal(SectionPairingStatus.SoftDeleted, SectionPairingReadiness.Classify(s));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Classifies_insufficient_players(int playerCount)
    {
        var s = BuildSection(playerCount: playerCount, finalRound: 4);
        Assert.Equal(SectionPairingStatus.InsufficientPlayers, SectionPairingReadiness.Classify(s));
    }

    [Fact]
    public void Withdrawn_players_dont_count_as_active()
    {
        var s = BuildSection(playerCount: 3, finalRound: 4);
        s = s with
        {
            Players = s.Players
                .Take(2).Select(p => p with { Withdrawn = true })
                .Concat(s.Players.Skip(2))
                .ToArray()
        };
        // 3 players, 2 withdrawn → 1 active → insufficient
        Assert.Equal(SectionPairingStatus.InsufficientPlayers, SectionPairingReadiness.Classify(s));
    }

    [Fact]
    public void Empty_section_is_ready_to_pair_round_one()
    {
        var s = BuildSection(playerCount: 4, finalRound: 4);
        Assert.Equal(SectionPairingStatus.ReadyToPair, SectionPairingReadiness.Classify(s));
        Assert.Equal(1, SectionPairingReadiness.NextRoundNumber(s));
    }

    [Fact]
    public void All_rounds_paired_is_terminal()
    {
        var s = BuildSection(playerCount: 4, finalRound: 2);
        s = s with
        {
            Rounds =
            [
                new Round(1, [new Pairing(1, 1, 2, PairingResult.WhiteWins)], []),
                new Round(2, [new Pairing(1, 1, 2, PairingResult.BlackWins)], []),
            ]
        };
        Assert.Equal(SectionPairingStatus.AllRoundsPaired, SectionPairingReadiness.Classify(s));
    }

    [Fact]
    public void Waiting_for_results_when_last_round_has_unplayed_pairing()
    {
        var s = BuildSection(playerCount: 4, finalRound: 4);
        s = s with
        {
            Rounds =
            [
                new Round(1, [new Pairing(1, 1, 2, PairingResult.Unplayed)], []),
            ]
        };
        Assert.Equal(SectionPairingStatus.WaitingForResults, SectionPairingReadiness.Classify(s));
    }

    [Fact]
    public void Ready_to_pair_round_two_when_round_one_complete_and_more_planned()
    {
        var s = BuildSection(playerCount: 4, finalRound: 4);
        s = s with
        {
            Rounds =
            [
                new Round(1, [new Pairing(1, 1, 2, PairingResult.WhiteWins)], []),
            ]
        };
        Assert.Equal(SectionPairingStatus.ReadyToPair, SectionPairingReadiness.Classify(s));
        Assert.Equal(2, SectionPairingReadiness.NextRoundNumber(s));
    }

    private static Section BuildSection(int playerCount, int finalRound)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => new Player(
                PairNumber: i,
                Name: $"Player {i}",
                UscfId: null,
                Rating: 1200,
                SecondaryRating: null,
                MembershipExpiration: null,
                Club: null,
                State: null,
                Team: null,
                RequestedByeRounds: [],
                History: []))
            .ToArray();

        return new Section(
            Name: "Test",
            Title: null,
            Kind: SectionKind.Swiss,
            TimeControl: null,
            RoundsPaired: 0,
            RoundsPlayed: 0,
            FinalRound: finalRound,
            FirstBoard: null,
            Players: players,
            Teams: [],
            Rounds: [],
            Prizes: new Prizes([], []));
    }
}
