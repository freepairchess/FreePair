using System.Linq;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests <see cref="BoardNumberRecommender"/> + the offset
/// application in <see cref="TournamentMutations.AppendRound"/> so
/// multi-section tournaments don't reuse the same physical board #1
/// across Open / U1700 / U1000 / etc.
/// </summary>
public class BoardNumberRecommenderTests
{
    [Fact]
    public void MaxBoardsNeeded_returns_ceiling_of_active_player_count_over_two()
    {
        Assert.Equal(0, BoardNumberRecommender.MaxBoardsNeeded(BuildSection("S", playerCount: 0)));
        Assert.Equal(1, BoardNumberRecommender.MaxBoardsNeeded(BuildSection("S", playerCount: 1)));
        Assert.Equal(1, BoardNumberRecommender.MaxBoardsNeeded(BuildSection("S", playerCount: 2)));
        Assert.Equal(2, BoardNumberRecommender.MaxBoardsNeeded(BuildSection("S", playerCount: 3)));
        Assert.Equal(25, BoardNumberRecommender.MaxBoardsNeeded(BuildSection("S", playerCount: 50)));
        Assert.Equal(26, BoardNumberRecommender.MaxBoardsNeeded(BuildSection("S", playerCount: 51)));
    }

    [Fact]
    public void MaxBoardsNeeded_excludes_withdrawn_and_softdeleted()
    {
        var section = BuildSection("S",
            playerCount: 10,
            customize: players => players
                .Take(2).Select(p => p with { Withdrawn = true })
                .Concat(players.Skip(2).Take(1).Select(p => p with { SoftDeleted = true }))
                .Concat(players.Skip(3))
                .ToArray());
        // 10 - 2 withdrawn - 1 soft-deleted = 7 active → ceil(7/2) = 4 boards.
        Assert.Equal(4, BoardNumberRecommender.MaxBoardsNeeded(section));
    }

    [Fact]
    public void Recommend_returns_cumulative_max_boards_walking_sections_in_order()
    {
        // User's worked example: 50-player Open + 30-player U1700 + 20-player U1200.
        // Open → start 1, max 25 boards, ends at 25.
        // U1700 → start 26, max 15 boards, ends at 40.
        // U1200 → start 41.
        var t = BuildTournament(
            ("Open",  50),
            ("U1700", 30),
            ("U1200", 20));

        var rec = BoardNumberRecommender.Recommend(t);

        Assert.Equal(1,  rec["Open"]);
        Assert.Equal(26, rec["U1700"]);
        Assert.Equal(41, rec["U1200"]);
    }

    [Fact]
    public void Recommend_skips_softdeleted_sections()
    {
        var t = BuildTournament(("Open", 10), ("Trash", 6), ("U1200", 8));
        // Soft-delete the middle section; recommendation should
        // collapse around it as if it didn't exist.
        var trash = t.Sections.Single(s => s.Name == "Trash");
        t = t with
        {
            Sections = t.Sections
                .Select(s => s.Name == "Trash" ? s with { SoftDeleted = true } : s)
                .ToArray()
        };

        var rec = BoardNumberRecommender.Recommend(t);

        Assert.False(rec.ContainsKey("Trash"));
        Assert.Equal(1, rec["Open"]);
        Assert.Equal(6, rec["U1200"]); // 1 + ceil(10/2) = 6
    }

    [Fact]
    public void AppendRound_offsets_board_numbers_by_FirstBoard_minus_one()
    {
        var t = BuildTournament(("Open", 4));
        // Set Open's first board to 26 — emulating section 2's slot.
        t = TournamentMutations.SetSectionFirstBoard(t, "Open", 26);

        var bbp = new BbpPairingResult(
            Pairings:
            [
                new BbpPairing(WhitePair: 1, BlackPair: 2),
                new BbpPairing(WhitePair: 3, BlackPair: 4),
            ],
            ByePlayerPairs: []);

        t = TournamentMutations.AppendRound(t, "Open", bbp);

        var round = t.Sections.Single().Rounds.Single();
        Assert.Equal([26, 27], round.Pairings.Select(p => p.Board).ToArray());
    }

    [Fact]
    public void AppendRound_uses_board_one_when_FirstBoard_is_null()
    {
        var t = BuildTournament(("Open", 4));
        Assert.Null(t.Sections.Single().FirstBoard);

        var bbp = new BbpPairingResult(
            Pairings:
            [
                new BbpPairing(WhitePair: 1, BlackPair: 2),
                new BbpPairing(WhitePair: 3, BlackPair: 4),
            ],
            ByePlayerPairs: []);

        t = TournamentMutations.AppendRound(t, "Open", bbp);

        Assert.Equal([1, 2], t.Sections.Single().Rounds.Single().Pairings.Select(p => p.Board).ToArray());
    }

    [Fact]
    public void SetSectionFirstBoard_rejects_zero_or_negative()
    {
        var t = BuildTournament(("Open", 4));
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            TournamentMutations.SetSectionFirstBoard(t, "Open", 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            TournamentMutations.SetSectionFirstBoard(t, "Open", -3));
    }

    [Fact]
    public void SetSectionFirstBoard_accepts_null_to_clear()
    {
        var t = BuildTournament(("Open", 4));
        t = TournamentMutations.SetSectionFirstBoard(t, "Open", 42);
        t = TournamentMutations.SetSectionFirstBoard(t, "Open", null);
        Assert.Null(t.Sections.Single().FirstBoard);
    }

    // ============ helpers ============

    private static Section BuildSection(
        string name,
        int playerCount,
        System.Func<Player[], Player[]>? customize = null)
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
        if (customize is not null) players = customize(players);

        return new Section(
            Name: name,
            Title: null,
            Kind: SectionKind.Swiss,
            TimeControl: null,
            RoundsPaired: 0,
            RoundsPlayed: 0,
            FinalRound: 4,
            FirstBoard: null,
            Players: players,
            Teams: [],
            Rounds: [],
            Prizes: new Prizes([], []));
    }

    private static Tournament BuildTournament(params (string Name, int PlayerCount)[] sections)
    {
        return new Tournament(
            Title: "Test",
            StartDate: null,
            EndDate: null,
            TimeControl: null,
            NachEventId: null,
            Sections: sections.Select(s => BuildSection(s.Name, s.PlayerCount)).ToArray());
    }
}
