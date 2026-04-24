using System.Linq;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.SwissSys;

/// <summary>
/// Tests for <see cref="RoundResultKind.ZeroPointBye"/> — the SwissSys
/// <c>"U"</c> prefix. The key invariants: score 0, round-trips through
/// <see cref="RoundResult.ToSwissSysToken"/> as <c>U</c>, tiebreaks and
/// standings treat it identically to an unpaired round (no phantom
/// opponent score), and <see cref="Section.IsWithdrawn"/> still detects
/// it as a withdrawal marker (opponent 0) for back-compat with files
/// written by older FreePair versions.
/// </summary>
public class ZeroPointByeTests
{
    [Fact]
    public void Parse_U_token_yields_ZeroPointBye_kind()
    {
        var r = RoundResult.Parse("U;0;-;0;0;0;0");
        Assert.Equal(RoundResultKind.ZeroPointBye, r.Kind);
        Assert.Equal(0m, r.Score);
    }

    [Fact]
    public void ZeroPointBye_roundtrips_as_U()
    {
        var r = new RoundResult(RoundResultKind.ZeroPointBye, 0, PlayerColor.None, 0, 0, 0, 0m);
        var token = r.ToSwissSysToken();
        Assert.StartsWith("U;", token);

        var r2 = RoundResult.Parse(token);
        Assert.Equal(r.Kind, r2.Kind);
    }

    [Fact]
    public void ZeroPointBye_counts_as_bye_in_IsBye()
    {
        var r = new RoundResult(RoundResultKind.ZeroPointBye, 0, PlayerColor.None, 0, 0, 0, 0m);
        Assert.True(r.IsBye);
    }

    [Fact]
    public void ZeroPointBye_does_not_count_as_unplayed_for_None_check()
    {
        // None is reserved for paired-but-unplayed or uninitialized slots.
        // ZeroPointBye is a distinct "TD assigned no game, no points" state.
        var r = new RoundResult(RoundResultKind.ZeroPointBye, 0, PlayerColor.None, 0, 0, 0, 0m);
        Assert.False(r.IsUnplayed);
    }

    [Fact]
    public void Section_IsWithdrawn_still_detects_U_marker()
    {
        // A player whose most recent round is "U;0;-;0;0;0;0" (zero-point
        // bye with no opponent) is treated as withdrawn by the heuristic
        // in Section.IsWithdrawn. This is the path SwissSys files use to
        // mark a TD-initiated forfeit / withdrawal round, so we must
        // preserve the detection even though we now parse U as
        // ZeroPointBye instead of None.
        var withdrawn = new RoundResult(RoundResultKind.ZeroPointBye, 0, PlayerColor.None, 0, 0, 0, 0m);
        var player = new Player(
            PairNumber: 1,
            Name: "Alice",
            UscfId: null,
            Rating: 1500,
            SecondaryRating: null,
            MembershipExpiration: null,
            Club: null,
            State: null,
            Team: null,
            RequestedByeRounds: System.Array.Empty<int>(),
            History: new[] { withdrawn });
        var section = new Section(
            Name: "Open",
            Title: null,
            Kind: SectionKind.Swiss,
            TimeControl: null,
            RoundsPaired: 1,
            RoundsPlayed: 1,
            FinalRound: 1,
            FirstBoard: 1,
            Players: new[] { player },
            Teams: System.Array.Empty<Team>(),
            Rounds: System.Array.Empty<Round>(),
            Prizes: new Prizes(System.Array.Empty<Prize>(), System.Array.Empty<Prize>()));

        Assert.True(section.IsWithdrawn(player));
    }
}
