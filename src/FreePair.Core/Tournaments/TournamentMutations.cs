using System;
using System.Collections.Generic;
using System.Linq;
using FreePair.Core.Bbp;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Pure functions that return a new <see cref="Tournament"/> with a single
/// logical change applied. Use to propagate TD edits and pairing-engine
/// output through the immutable domain graph.
/// </summary>
public static class TournamentMutations
{
    /// <summary>
    /// Returns a new tournament in which the pairing identified by the
    /// given <paramref name="sectionName"/>, <paramref name="round"/> and
    /// (<paramref name="whitePair"/>, <paramref name="blackPair"/>) has
    /// the supplied <paramref name="result"/>. Also updates both players'
    /// <see cref="Player.History"/> entries so scores/tiebreaks stay
    /// consistent.
    /// </summary>
    public static Tournament SetPairingResult(
        Tournament tournament,
        string sectionName,
        int round,
        int whitePair,
        int blackPair,
        PairingResult result)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        var roundIndex = round - 1;
        if (roundIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(round), "Round is 1-based.");
        }

        var targetRound = section.Rounds.FirstOrDefault(r => r.Number == round)
            ?? throw new InvalidOperationException($"Round {round} does not exist in section '{sectionName}'.");

        var updatedPairings = targetRound.Pairings
            .Select(p => (p.WhitePair == whitePair && p.BlackPair == blackPair)
                ? p with { Result = result }
                : p)
            .ToArray();
        var updatedRound = targetRound with { Pairings = updatedPairings };

        var updatedRounds = section.Rounds
            .Select(r => r.Number == round ? updatedRound : r)
            .ToArray();

        var updatedPlayers = section.Players
            .Select(p => UpdatePlayerHistory(p, roundIndex, whitePair, blackPair, result))
            .ToArray();

        var updatedSection = section with
        {
            Players = updatedPlayers,
            Rounds = updatedRounds,
            RoundsPlayed = RecomputeRoundsPlayed(updatedRounds),
        };

        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    /// <summary>
    /// Returns a new tournament with an additional round appended to the
    /// given section, populated from BBP's output. The new round's pairings
    /// have <see cref="PairingResult.Unplayed"/>; bye recipients receive a
    /// full-point bye.
    /// </summary>
    public static Tournament AppendRound(
        Tournament tournament,
        string sectionName,
        BbpPairingResult pairings)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(pairings);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        var newRoundNumber = section.Rounds.Count + 1;

        var boardPairings = pairings.Pairings
            .Select((p, i) => new Pairing(i + 1, p.WhitePair, p.BlackPair, PairingResult.Unplayed))
            .ToArray();

        var fullByeAssignments = pairings.ByePlayerPairs
            .Select(pair => new ByeAssignment(pair, ByeKind.Full));
        var halfByeAssignments = pairings.HalfPointByes
            .Select(pair => new ByeAssignment(pair, ByeKind.Half));
        var byeAssignments = fullByeAssignments
            .Concat(halfByeAssignments)
            .ToArray();

        var newRound = new Round(newRoundNumber, boardPairings, byeAssignments);

        var byPair = section.Players.ToDictionary(p => p.PairNumber);
        var pairingByPlayer = new Dictionary<int, (int Opponent, PlayerColor Color, int Board)>();
        foreach (var bp in boardPairings)
        {
            pairingByPlayer[bp.WhitePair] = (bp.BlackPair, PlayerColor.White, bp.Board);
            pairingByPlayer[bp.BlackPair] = (bp.WhitePair, PlayerColor.Black, bp.Board);
        }
        var fullByeSet = new HashSet<int>(pairings.ByePlayerPairs);
        var halfByeSet = new HashSet<int>(pairings.HalfPointByes);

        var updatedPlayers = section.Players
            .Select(p => p.Withdrawn
                ? p                                  // session-withdrawn → leave history at its current length
                : AppendHistoryEntry(p, pairingByPlayer, fullByeSet, halfByeSet))
            .ToArray();

        var updatedSection = section with
        {
            Players = updatedPlayers,
            Rounds = section.Rounds.Append(newRound).ToArray(),
            RoundsPaired = Math.Max(section.RoundsPaired, newRoundNumber),
        };

        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    /// <summary>
    /// Returns <c>true</c> when every pairing in the round has a final
    /// (non-<see cref="PairingResult.Unplayed"/>) result.
    /// </summary>
    public static bool IsRoundComplete(Section section, int round)
    {
        ArgumentNullException.ThrowIfNull(section);

        var r = section.Rounds.FirstOrDefault(x => x.Number == round);
        return r is not null && r.Pairings.All(p => p.Result != PairingResult.Unplayed);
    }

    /// <summary>
    /// Sets the <see cref="Player.Withdrawn"/> flag on a single player
    /// in <paramref name="sectionName"/>. Withdrawn players are skipped
    /// by <see cref="Trf.TrfWriter.Write"/> (they disappear from BBP's
    /// pool) and by <see cref="AppendRound"/> (their history is not
    /// extended), while their existing history remains intact so the
    /// wall chart / standings / tiebreaks continue to reflect any games
    /// they did play. Pass <c>false</c> to re-activate a player.
    /// </summary>
    public static Tournament SetPlayerWithdrawn(
        Tournament tournament,
        string sectionName,
        int pairNumber,
        bool withdrawn)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        var target = section.Players.FirstOrDefault(p => p.PairNumber == pairNumber)
            ?? throw new InvalidOperationException(
                $"Player with pair #{pairNumber} not found in section '{sectionName}'.");

        if (target.Withdrawn == withdrawn)
        {
            return tournament; // no-op
        }

        var updatedPlayers = section.Players
            .Select(p => p.PairNumber == pairNumber ? p with { Withdrawn = withdrawn } : p)
            .ToArray();
        var updatedSection = section with { Players = updatedPlayers };
        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    /// <summary>
    /// Returns a new tournament with the last round of the given section
    /// removed (its pairings, its byes, and each player's most recent
    /// history entry). Intended for correcting a mispaired or mis-recorded
    /// round: the TD can delete it and re-run <c>Pair next round</c> or
    /// re-enter results.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The section has no rounds to delete.
    /// </exception>
    public static Tournament DeleteLastRound(Tournament tournament, string sectionName)    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        if (section.Rounds.Count == 0)
        {
            throw new InvalidOperationException(
                $"Section '{sectionName}' has no rounds to delete.");
        }

        var updatedRounds = section.Rounds.Take(section.Rounds.Count - 1).ToArray();

        var updatedPlayers = section.Players
            .Select(p => p.History.Count == 0
                ? p
                : p with { History = p.History.Take(p.History.Count - 1).ToArray() })
            .ToArray();

        var updatedSection = section with
        {
            Players = updatedPlayers,
            Rounds = updatedRounds,
            RoundsPaired = updatedRounds.Length,
            RoundsPlayed = RecomputeRoundsPlayed(updatedRounds),
        };

        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    private static Section FindSection(Tournament t, string name) =>
        t.Sections.FirstOrDefault(s => s.Name == name)
        ?? throw new InvalidOperationException($"Section '{name}' not found.");

    private static Tournament ReplaceSection(Tournament t, string name, Section replacement)
    {
        var newSections = t.Sections
            .Select(s => s.Name == name ? replacement : s)
            .ToArray();
        return t with { Sections = newSections };
    }

    private static int RecomputeRoundsPlayed(IReadOnlyList<Round> rounds)
    {
        var played = 0;
        foreach (var r in rounds.OrderBy(x => x.Number))
        {
            if (r.Pairings.All(p => p.Result != PairingResult.Unplayed))
            {
                played = r.Number;
            }
            else
            {
                break;
            }
        }
        return played;
    }

    private static Player UpdatePlayerHistory(
        Player player,
        int roundIndex,
        int whitePair,
        int blackPair,
        PairingResult result)
    {
        if (player.PairNumber != whitePair && player.PairNumber != blackPair)
        {
            return player;
        }

        if (roundIndex < 0 || roundIndex >= player.History.Count)
        {
            return player;
        }

        var existing = player.History[roundIndex];
        var isWhite = player.PairNumber == whitePair;
        var opponent = isWhite ? blackPair : whitePair;
        var color = isWhite ? PlayerColor.White : PlayerColor.Black;

        var kind = result switch
        {
            PairingResult.WhiteWins => isWhite ? RoundResultKind.Win  : RoundResultKind.Loss,
            PairingResult.BlackWins => isWhite ? RoundResultKind.Loss : RoundResultKind.Win,
            PairingResult.Draw      => RoundResultKind.Draw,
            _                       => RoundResultKind.None,
        };

        var newHistory = player.History.ToArray();
        newHistory[roundIndex] = new RoundResult(
            Kind: kind,
            Opponent: opponent,
            Color: color,
            Board: existing.Board,
            Logic1: existing.Logic1,
            Logic2: existing.Logic2,
            GamePoints: existing.GamePoints);

        return player with { History = newHistory };
    }

    private static Player AppendHistoryEntry(
        Player player,
        IReadOnlyDictionary<int, (int Opponent, PlayerColor Color, int Board)> pairingByPlayer,
        IReadOnlySet<int> fullByeSet,
        IReadOnlySet<int> halfByeSet)
    {
        RoundResult entry;
        if (pairingByPlayer.TryGetValue(player.PairNumber, out var info))
        {
            // Paired game awaiting result: Kind=None with a real opponent
            // is our convention for "pairing set, no result yet". Tiebreak
            // calculations are bounded by RoundsPlayed so this never leaks
            // into standings until the result is entered.
            entry = new RoundResult(
                Kind: RoundResultKind.None,
                Opponent: info.Opponent,
                Color: info.Color,
                Board: info.Board,
                Logic1: 0,
                Logic2: 0,
                GamePoints: 0m);
        }
        else if (fullByeSet.Contains(player.PairNumber))
        {
            entry = new RoundResult(
                Kind: RoundResultKind.FullPointBye,
                Opponent: -1,
                Color: PlayerColor.None,
                Board: 0,
                Logic1: 0,
                Logic2: 0,
                GamePoints: 1m);
        }
        else if (halfByeSet.Contains(player.PairNumber))
        {
            // Requested / TD-granted half-point bye (pre-flagged via the
            // TRF 'H' cell so BBP never attempted to pair them this round).
            entry = new RoundResult(
                Kind: RoundResultKind.HalfPointBye,
                Opponent: -1,
                Color: PlayerColor.None,
                Board: 0,
                Logic1: 0,
                Logic2: 0,
                GamePoints: 0.5m);
        }
        else
        {
            // Player was not included in BBP output (withdrawal etc.) —
            // record an unpaired cell.
            entry = new RoundResult(
                Kind: RoundResultKind.None,
                Opponent: 0,
                Color: PlayerColor.None,
                Board: 0,
                Logic1: 0,
                Logic2: 0,
                GamePoints: 0m);
        }

        return player with { History = player.History.Append(entry).ToArray() };
    }
}
