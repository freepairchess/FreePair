using System;
using System.Collections.Generic;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;

namespace FreePair.Core.Tests.Uscf;

/// <summary>
/// Round-1 pairing tests for <see cref="UscfPairer"/>. USCF rule 28C:
/// rank by rating, split top half vs bottom half, alternate colours
/// from the round-1 assigned colour (TRF <c>XXC</c>).
/// </summary>
public class UscfPairerRound1Tests
{
    private static TrfDocument MakeDoc(char initialColor, params (int pair, int rating)[] players)
    {
        var list = new List<TrfPlayer>(players.Length);
        foreach (var (pair, rating) in players)
        {
            list.Add(new TrfPlayer(
                PairNumber: pair,
                Name: $"Player{pair}",
                Rating: rating,
                Id: string.Empty,
                Points: 0m,
                Rounds: Array.Empty<TrfRoundCell>()));
        }
        return new TrfDocument(
            TournamentName: "Test",
            StartDate: string.Empty,
            EndDate: string.Empty,
            TotalRounds: 5,
            InitialColor: initialColor,
            Players: list);
    }

    [Fact]
    public void Even_field_pairs_top_half_against_bottom_half_by_rating()
    {
        // 8 players: rank 1 (highest rating) plays rank 5; 2v6; 3v7; 4v8.
        var doc = MakeDoc('w',
            (1, 2200),
            (2, 2100),
            (3, 2000),
            (4, 1900),
            (5, 1800),
            (6, 1700),
            (7, 1600),
            (8, 1500));

        var result = UscfPairer.Pair(doc);

        Assert.Null(result.ByePair);
        Assert.Equal(4, result.Pairings.Count);

        // Board 1: top seed (pair 1) plays bottom of top half (pair 5).
        // White1 means seed 1 plays white on board 1.
        Assert.Equal(new UscfPairing(1, 5, 1), result.Pairings[0]);
        // Board 2: top seed (pair 2) plays seed 6, but colour alternates
        // — pair 6 plays white, pair 2 plays black.
        Assert.Equal(new UscfPairing(6, 2, 2), result.Pairings[1]);
        Assert.Equal(new UscfPairing(3, 7, 3), result.Pairings[2]);
        Assert.Equal(new UscfPairing(8, 4, 4), result.Pairings[3]);
    }

    [Fact]
    public void Black1_initial_color_flips_the_alternation()
    {
        var doc = MakeDoc('b',
            (1, 2000),
            (2, 1900),
            (3, 1800),
            (4, 1700));

        var result = UscfPairer.Pair(doc);

        Assert.Null(result.ByePair);
        Assert.Equal(2, result.Pairings.Count);
        // black1 → top seed of board 1 is BLACK. So pair 3 (bottom half)
        // is white and pair 1 (top seed) is black.
        Assert.Equal(new UscfPairing(3, 1, 1), result.Pairings[0]);
        // Board 2 alternates → top seed (pair 2) gets white, pair 4 black.
        Assert.Equal(new UscfPairing(2, 4, 2), result.Pairings[1]);
    }

    [Fact]
    public void Odd_field_gives_lowest_rated_player_the_bye()
    {
        var doc = MakeDoc('w',
            (1, 1900),
            (2, 1800),
            (3, 1700),
            (4, 1600),
            (5, 1500));

        var result = UscfPairer.Pair(doc);

        Assert.Equal(5, result.ByePair);
        Assert.Equal(2, result.Pairings.Count);
        Assert.Equal(new UscfPairing(1, 3, 1), result.Pairings[0]);
        Assert.Equal(new UscfPairing(4, 2, 2), result.Pairings[1]);
    }

    [Fact]
    public void Rating_ties_break_by_pair_number_ascending()
    {
        // All identical ratings → starting rank decides.
        var doc = MakeDoc('w',
            (1, 1500),
            (2, 1500),
            (3, 1500),
            (4, 1500));

        var result = UscfPairer.Pair(doc);

        Assert.Equal(2, result.Pairings.Count);
        Assert.Equal(new UscfPairing(1, 3, 1), result.Pairings[0]);
        Assert.Equal(new UscfPairing(4, 2, 2), result.Pairings[1]);
    }

    [Fact]
    public void Roster_order_does_not_change_pairings()
    {
        // Same players, scrambled input order — output must be identical
        // because the pairer always sorts by rating then pair number.
        var ordered = MakeDoc('w',
            (1, 2000), (2, 1900), (3, 1800), (4, 1700));
        var scrambled = MakeDoc('w',
            (3, 1800), (1, 2000), (4, 1700), (2, 1900));

        var a = UscfPairer.Pair(ordered);
        var b = UscfPairer.Pair(scrambled);

        Assert.Equal(a.Pairings, b.Pairings);
        Assert.Equal(a.ByePair, b.ByePair);
    }

    [Fact]
    public void Empty_field_yields_empty_result()
    {
        var doc = MakeDoc('w');
        var result = UscfPairer.Pair(doc);
        Assert.Empty(result.Pairings);
        Assert.Null(result.ByePair);
    }

    [Fact]
    public void Round_2_pairing_throws_NotImplemented_for_now()
    {
        // Document with one round of history → engine must refuse with a
        // clear message instead of silently producing wrong pairings.
        var docWithHistory = new TrfDocument(
            TournamentName: "Test",
            StartDate: string.Empty,
            EndDate: string.Empty,
            TotalRounds: 5,
            InitialColor: 'w',
            Players:
            [
                new TrfPlayer(1, "A", 2000, "", 1m,
                    [ new TrfRoundCell(Opponent: 2, Color: 'w', Result: '1') ]),
                new TrfPlayer(2, "B", 1900, "", 0m,
                    [ new TrfRoundCell(Opponent: 1, Color: 'b', Result: '0') ]),
            ]);

        Assert.Throws<NotImplementedException>(() => UscfPairer.Pair(docWithHistory));
    }
}
