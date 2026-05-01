using System;
using System.Collections.Generic;
using System.Linq;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Deterministic round-robin (all-play-all) scheduler using the classic
/// Berger circle method. Given <c>N</c> players the schedule has
/// <c>N - 1</c> rounds when <c>N</c> is even and <c>N</c> rounds when
/// <c>N</c> is odd (each player sits out exactly once on a full-point
/// bye). Colours alternate per player from round to round so that each
/// player gets as balanced a W/B split as mathematically possible.
/// </summary>
/// <remarks>
/// <para>
/// Player positions are modelled as a fixed "anchor" (position 0) and
/// <c>M-1</c> rotating positions 1..M-1, where <c>M</c> is the number
/// of playing seats (<c>M = N</c> when <c>N</c> is even,
/// <c>M = N + 1</c> when <c>N</c> is odd — the extra seat is the phantom
/// "bye" and yields a full-point bye for whoever faces it).
/// </para>
/// <para>
/// In round <c>r</c> (0-indexed), the anchor plays the player at
/// rotated position <c>r mod (M-1) + 1</c>; the remaining seats pair up
/// outside-in. Colours are determined by seat index so that a player's
/// sequence is roughly WBWB… from one round to the next.
/// </para>
/// <para>
/// Callers are expected to number their players 1..N in the desired
/// seating order (typically by rating, descending). The resulting
/// <see cref="Round"/>s use <see cref="Pairing.WhitePair"/> and
/// <see cref="Pairing.BlackPair"/> as those pair numbers; a pairing
/// whose opponent is <c>0</c> indicates a bye for that round.
/// </para>
/// </remarks>
public static class RoundRobinScheduler
{
    /// <summary>
    /// Builds the full set of rounds for a round-robin with the given
    /// ordered pair numbers.
    /// </summary>
    /// <param name="pairNumbers">
    /// Seating order of players 1..N. Must contain at least 2 distinct
    /// positive integers.
    /// </param>
    /// <param name="firstBoard">
    /// Physical board number of this section's first board. Boards are
    /// emitted as <c>firstBoard</c>, <c>firstBoard + 1</c>, ... so
    /// downstream sections in a multi-section event don't reuse the
    /// same physical board #1. Defaults to 1 (legacy single-section
    /// behaviour). Values ≤ 0 are clamped to 1.
    /// </param>
    /// <returns>
    /// One <see cref="Round"/> per schedule round. Each round's
    /// <see cref="Round.Pairings"/> is non-empty and covers every
    /// (non-bye) player exactly once; each round's
    /// <see cref="Round.Byes"/> carries at most one entry (the player
    /// matched against the phantom seat when <c>N</c> is odd).
    /// </returns>
    public static IReadOnlyList<Round> Build(IReadOnlyList<int> pairNumbers, int firstBoard = 1)
    {
        ArgumentNullException.ThrowIfNull(pairNumbers);
        if (pairNumbers.Count < 2)
        {
            throw new ArgumentException(
                "A round-robin needs at least 2 players.", nameof(pairNumbers));
        }
        if (pairNumbers.Distinct().Count() != pairNumbers.Count)
        {
            throw new ArgumentException(
                "Pair numbers must be distinct.", nameof(pairNumbers));
        }

        var boardOffset = (firstBoard <= 0 ? 1 : firstBoard) - 1;

        // Phantom seat (value 0) padding for odd player counts.
        var seats = pairNumbers.ToList();
        var hasPhantom = seats.Count % 2 == 1;
        if (hasPhantom)
        {
            seats.Add(0);
        }

        var totalSeats = seats.Count;                 // always even
        var rotatingCount = totalSeats - 1;
        var pairsPerRound = totalSeats / 2;
        var rounds = new List<Round>(rotatingCount);

        // Running colour counts used by the greedy assigner below. Every
        // round-robin pairing has exactly one "more balanced" colour
        // choice; greedily picking it produces a schedule where every
        // player's W-vs-B count differs by at most one — the tightest
        // possible for round-robin.
        var whiteCount = pairNumbers.ToDictionary(p => p, _ => 0);
        var blackCount = pairNumbers.ToDictionary(p => p, _ => 0);

        for (var r = 0; r < rotatingCount; r++)
        {
            // Rotate positions 1..totalSeats-1 by r. Position 0 (anchor)
            // stays put. "Seat" indices are positions in the rotation
            // circle; the actual pair numbers live in 'rotated[]'.
            var rotated = new int[totalSeats];
            rotated[0] = seats[0];
            for (var i = 1; i < totalSeats; i++)
            {
                var from = ((i - 1 + r) % rotatingCount) + 1;
                rotated[i] = seats[from];
            }

            var pairings = new List<Pairing>(pairsPerRound);
            var byes = new List<ByeAssignment>(1);

            for (var board = 0; board < pairsPerRound; board++)
            {
                // Outside-in pairing: seat[board] vs seat[totalSeats-1-board].
                var left = rotated[board];
                var right = rotated[totalSeats - 1 - board];

                if (left == 0 || right == 0)
                {
                    // The non-phantom gets a full-point bye this round.
                    var realPlayer = left == 0 ? right : left;
                    byes.Add(new ByeAssignment(realPlayer, ByeKind.Full));
                    continue;
                }

                // Greedy colour: white goes to whichever player has fewer
                // whites so far; ties broken by who has more blacks
                // (they're the one "owed" white); further ties by lower
                // pair number so the schedule is deterministic.
                var white = ChooseWhite(left, right, whiteCount, blackCount);
                var black = white == left ? right : left;

                whiteCount[white]++;
                blackCount[black]++;

                pairings.Add(new Pairing(
                    Board: board + 1 + boardOffset,
                    WhitePair: white,
                    BlackPair: black,
                    Result: PairingResult.Unplayed));
            }

            rounds.Add(new Round(r + 1, pairings, byes));
        }

        return rounds;
    }

    private static int ChooseWhite(
        int a,
        int b,
        IReadOnlyDictionary<int, int> whiteCount,
        IReadOnlyDictionary<int, int> blackCount)
    {
        if (whiteCount[a] != whiteCount[b])
        {
            return whiteCount[a] < whiteCount[b] ? a : b;
        }
        if (blackCount[a] != blackCount[b])
        {
            return blackCount[a] > blackCount[b] ? a : b;
        }
        return a < b ? a : b;
    }
}
