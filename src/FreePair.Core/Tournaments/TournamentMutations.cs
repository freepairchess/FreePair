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
    /// Applies a batch of event-level metadata edits in a single
    /// immutable step. Any parameter left at its default sentinel
    /// (<see langword="null"/>) is treated as "leave unchanged". Used
    /// by the Event-configuration tab so the TD can edit several
    /// fields and apply them as one unit.
    /// </summary>
    public static Tournament SetTournamentInfo(
        Tournament tournament,
        string? title = null,
        System.DateOnly? startDate = null,
        System.DateOnly? endDate = null,
        string? timeControl = null,
        // address
        string? eventAddress = null,
        string? eventCity = null,
        string? eventState = null,
        string? eventZipCode = null,
        string? eventCountry = null,
        // classifications — use Box<T> so the caller can distinguish
        // "leave alone" (null box) from "set to null" (non-null box
        // with Value == null).
        Box<FreePair.Core.Tournaments.Enums.EventFormat?>?     eventFormat     = null,
        Box<FreePair.Core.Tournaments.Enums.EventType?>?       eventType       = null,
        Box<FreePair.Core.Tournaments.Enums.PairingRule?>?     pairingRule     = null,
        Box<FreePair.Core.Tournaments.Enums.TimeControlType?>? timeControlType = null,
        Box<FreePair.Core.Tournaments.Enums.RatingType?>?      ratingType      = null,
        // extended
        string? organizerId = null,
        Box<FreePair.Core.Tournaments.Enums.UserIDType?>? organizerIdType = null,
        string? organizerName = null,
        string? nachPasscode = null,
        System.DateTimeOffset? startDateTime = null,
        System.DateTimeOffset? endDateTime = null,
        string? timeZone = null,
        int? roundsPlanned = null,
        int? halfPointByesAllowed = null,
        Box<bool?>? autoPublishPairings = null,
        Box<bool?>? autoPublishResults  = null,
        Box<System.DateTimeOffset?>? lastPublishedAt = null)
    {
        ArgumentNullException.ThrowIfNull(tournament);

        return tournament with
        {
            Title              = title           ?? tournament.Title,
            StartDate          = startDate       ?? tournament.StartDate,
            EndDate            = endDate         ?? tournament.EndDate,
            TimeControl        = timeControl     ?? tournament.TimeControl,

            EventAddress       = eventAddress    ?? tournament.EventAddress,
            EventCity          = eventCity       ?? tournament.EventCity,
            EventState         = eventState      ?? tournament.EventState,
            EventZipCode       = eventZipCode    ?? tournament.EventZipCode,
            EventCountry       = eventCountry    ?? tournament.EventCountry,

            EventFormat        = eventFormat     is not null ? eventFormat.Value     : tournament.EventFormat,
            EventType          = eventType       is not null ? eventType.Value       : tournament.EventType,
            PairingRule        = pairingRule     is not null ? pairingRule.Value     : tournament.PairingRule,
            TimeControlType    = timeControlType is not null ? timeControlType.Value : tournament.TimeControlType,
            RatingType         = ratingType      is not null ? ratingType.Value      : tournament.RatingType,

            OrganizerId        = organizerId     ?? tournament.OrganizerId,
            OrganizerIdType    = organizerIdType is not null ? organizerIdType.Value : tournament.OrganizerIdType,
            OrganizerName      = organizerName   ?? tournament.OrganizerName,
            NachPasscode       = nachPasscode    ?? tournament.NachPasscode,
            StartDateTime      = startDateTime   ?? tournament.StartDateTime,
            EndDateTime        = endDateTime     ?? tournament.EndDateTime,
            TimeZone           = timeZone        ?? tournament.TimeZone,
            RoundsPlanned      = roundsPlanned   ?? tournament.RoundsPlanned,
            HalfPointByesAllowed = halfPointByesAllowed ?? tournament.HalfPointByesAllowed,
            AutoPublishPairings = autoPublishPairings is not null ? autoPublishPairings.Value : tournament.AutoPublishPairings,
            AutoPublishResults  = autoPublishResults  is not null ? autoPublishResults.Value  : tournament.AutoPublishResults,
            LastPublishedAt     = lastPublishedAt     is not null ? lastPublishedAt.Value     : tournament.LastPublishedAt,
        };
    }

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
        GuardNoSoftDeletedPlayers(section);
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
    /// Toggles the section-level <see cref="Section.AvoidSameTeam"/>
    /// / <see cref="Section.AvoidSameClub"/> flags. When enabled,
    /// <see cref="PairingSwapper"/> post-processes BBP's output and
    /// attempts same-score-group swaps to avoid pairings between
    /// players sharing the corresponding field.
    /// </summary>
    public static Tournament SetAvoidSameTeam(
        Tournament tournament, string sectionName, bool avoid) =>
        UpdateSection(tournament, sectionName, s =>
            s.AvoidSameTeam == avoid ? s : s with { AvoidSameTeam = avoid });

    /// <inheritdoc cref="SetAvoidSameTeam"/>
    public static Tournament SetAvoidSameClub(
        Tournament tournament, string sectionName, bool avoid) =>
        UpdateSection(tournament, sectionName, s =>
            s.AvoidSameClub == avoid ? s : s with { AvoidSameClub = avoid });

    /// <summary>
    /// Adds an unordered <c>(A, B)</c> pair to the section's
    /// do-not-pair blacklist (used by
    /// <see cref="Constraints.DoNotPairConstraint"/>). Duplicates are
    /// collapsed; self-pairs are silently rejected.
    /// </summary>
    public static Tournament AddDoNotPair(
        Tournament tournament, string sectionName, int a, int b)
    {
        if (a == b) return tournament;
        var lo = System.Math.Min(a, b);
        var hi = System.Math.Max(a, b);
        return UpdateSection(tournament, sectionName, s =>
        {
            if (s.DoNotPairs.Any(p => p.A == lo && p.B == hi)) return s;
            var updated = s.DoNotPairs.Append((lo, hi)).ToArray();
            return s with { DoNotPairPairs = updated };
        });
    }

    /// <summary>
    /// Removes an unordered <c>(A, B)</c> pair from the section's
    /// do-not-pair blacklist. No-op when the pair isn't present.
    /// </summary>
    public static Tournament RemoveDoNotPair(
        Tournament tournament, string sectionName, int a, int b)
    {
        var lo = System.Math.Min(a, b);
        var hi = System.Math.Max(a, b);
        return UpdateSection(tournament, sectionName, s =>
        {
            var updated = s.DoNotPairs
                .Where(p => !(p.A == lo && p.B == hi))
                .ToArray();
            if (updated.Length == s.DoNotPairs.Count) return s;
            return s with { DoNotPairPairs = updated };
        });
    }

    /// <summary>
    /// Adds a <see cref="ForcedPairing"/> to the section. The two
    /// players will be withheld from the pairing engine for that
    /// round and placed on board 1 (or subsequent boards when
    /// multiple forced pairings exist for the same round). A duplicate
    /// (same round + same unordered pair) is collapsed; a pairing in
    /// which one of the players is already forced on that round
    /// throws because the conflict cannot be resolved silently.
    /// </summary>
    public static Tournament AddForcedPairing(
        Tournament tournament,
        string sectionName,
        int round,
        int whitePair,
        int blackPair)
    {
        if (whitePair == blackPair)
        {
            throw new ArgumentException(
                "A forced pairing needs two distinct pair numbers.",
                nameof(blackPair));
        }

        return UpdateSection(tournament, sectionName, s =>
        {
            var existing = s.ForcedPairs;
            foreach (var f in existing.Where(f => f.Round == round))
            {
                // Identical (unordered) pair already on this round → no-op.
                if ((f.WhitePair == whitePair && f.BlackPair == blackPair) ||
                    (f.WhitePair == blackPair && f.BlackPair == whitePair))
                {
                    return s;
                }
                // Either player is already forced against somebody else.
                if (f.WhitePair == whitePair || f.BlackPair == whitePair
                 || f.WhitePair == blackPair || f.BlackPair == blackPair)
                {
                    throw new InvalidOperationException(
                        $"Pair #{whitePair} or #{blackPair} is already part of a forced pairing for round {round}.");
                }
            }

            var updated = existing.Append(new ForcedPairing(round, whitePair, blackPair)).ToArray();
            return s with { ForcedPairings = updated };
        });
    }

    /// <summary>
    /// Removes the forced pairing matching <paramref name="round"/>
    /// and the unordered <c>(whitePair, blackPair)</c>. No-op when no
    /// such pairing is configured.
    /// </summary>
    public static Tournament RemoveForcedPairing(
        Tournament tournament,
        string sectionName,
        int round,
        int whitePair,
        int blackPair)
    {
        return UpdateSection(tournament, sectionName, s =>
        {
            var updated = s.ForcedPairs
                .Where(f => !(f.Round == round &&
                              ((f.WhitePair == whitePair && f.BlackPair == blackPair) ||
                               (f.WhitePair == blackPair && f.BlackPair == whitePair))))
                .ToArray();
            if (updated.Length == s.ForcedPairs.Count) return s;
            return s with { ForcedPairings = updated };
        });
    }

    /// <summary>
    /// Swaps colours on a single un-played pairing: the current white
    /// player becomes black and vice-versa. Used by the TD when
    /// overriding bbpPairings' colour allocation (e.g. to correct for
    /// an in-person colour-history preference the engine couldn't see).
    /// Both players' round-history entries are updated to reflect the
    /// new colour. Throws when the pairing doesn't exist or has
    /// already been scored.
    /// </summary>
    public static Tournament SwapPairingColors(
        Tournament tournament,
        string sectionName,
        int round,
        int whitePair,
        int blackPair)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        var targetRound = section.Rounds.FirstOrDefault(r => r.Number == round)
            ?? throw new InvalidOperationException(
                $"Round {round} does not exist in section '{sectionName}'.");

        var pairing = targetRound.Pairings.FirstOrDefault(
            p => p.WhitePair == whitePair && p.BlackPair == blackPair)
            ?? throw new InvalidOperationException(
                $"Pairing {whitePair}w vs {blackPair}b not found in round {round}.");

        if (pairing.Result != PairingResult.Unplayed)
        {
            throw new InvalidOperationException(
                $"Pairing {whitePair} vs {blackPair} has already been scored; colours can't be swapped.");
        }

        var swapped = pairing with { WhitePair = blackPair, BlackPair = whitePair };
        var updatedRound = targetRound with
        {
            Pairings = targetRound.Pairings
                .Select(p => p == pairing ? swapped : p)
                .ToArray(),
        };

        var roundIndex = round - 1;
        var updatedPlayers = section.Players
            .Select(p => p.PairNumber == whitePair || p.PairNumber == blackPair
                ? FlipHistoryColor(p, roundIndex)
                : p)
            .ToArray();

        var updatedSection = section with
        {
            Players = updatedPlayers,
            Rounds = section.Rounds.Select(r => r.Number == round ? updatedRound : r).ToArray(),
        };

        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    /// <summary>
    /// Swaps the black players across two un-played pairings on the
    /// same round: <c>(Aw, Ab)</c> and <c>(Bw, Bb)</c> become
    /// <c>(Aw, Bb)</c> and <c>(Bw, Ab)</c>. Colours are preserved so
    /// FIDE C.04 allocation is not disturbed. Throws when either
    /// pairing is already scored, or when the swap would recreate a
    /// previously-played game.
    /// </summary>
    public static Tournament SwapBoardOpponents(
        Tournament tournament,
        string sectionName,
        int round,
        int boardA,
        int boardB)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        if (boardA == boardB)
        {
            throw new ArgumentException("boardA and boardB must differ.", nameof(boardB));
        }

        var section = FindSection(tournament, sectionName);
        var targetRound = section.Rounds.FirstOrDefault(r => r.Number == round)
            ?? throw new InvalidOperationException(
                $"Round {round} does not exist in section '{sectionName}'.");

        var a = targetRound.Pairings.FirstOrDefault(p => p.Board == boardA)
            ?? throw new InvalidOperationException($"Board {boardA} not found in round {round}.");
        var b = targetRound.Pairings.FirstOrDefault(p => p.Board == boardB)
            ?? throw new InvalidOperationException($"Board {boardB} not found in round {round}.");

        if (a.Result != PairingResult.Unplayed || b.Result != PairingResult.Unplayed)
        {
            throw new InvalidOperationException(
                "Both boards must be un-played before swapping opponents.");
        }

        // Guard against rematches.
        var roundIndex = round - 1;
        var byPair = section.Players.ToDictionary(p => p.PairNumber);
        if (HasPlayedBefore(byPair, a.WhitePair, b.BlackPair, roundIndex) ||
            HasPlayedBefore(byPair, b.WhitePair, a.BlackPair, roundIndex))
        {
            throw new InvalidOperationException(
                "Swap would recreate a previously-played pairing.");
        }

        var newA = a with { BlackPair = b.BlackPair };
        var newB = b with { BlackPair = a.BlackPair };

        var updatedPairings = targetRound.Pairings
            .Select(p => p == a ? newA : p == b ? newB : p)
            .ToArray();
        var updatedRound = targetRound with { Pairings = updatedPairings };

        // Update history for the four affected players.
        var affected = new Dictionary<int, (int Opponent, PlayerColor Color, int Board)>
        {
            [a.WhitePair] = (b.BlackPair, PlayerColor.White, a.Board),
            [b.BlackPair] = (a.WhitePair, PlayerColor.Black, a.Board),
            [b.WhitePair] = (a.BlackPair, PlayerColor.White, b.Board),
            [a.BlackPair] = (b.WhitePair, PlayerColor.Black, b.Board),
        };

        var updatedPlayers = section.Players
            .Select(p => affected.TryGetValue(p.PairNumber, out var info)
                ? OverwriteHistoryPairing(p, roundIndex, info.Opponent, info.Color, info.Board)
                : p)
            .ToArray();

        var updatedSection = section with
        {
            Players = updatedPlayers,
            Rounds = section.Rounds.Select(r => r.Number == round ? updatedRound : r).ToArray(),
        };

        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    /// <summary>
    /// Converts a scheduled but un-played pairing into a late
    /// half-point bye for <paramref name="halfByePair"/>: that player
    /// is awarded 0.5, their opponent receives a full-point bye
    /// (1.0), the pairing is removed from the round, and two
    /// <see cref="ByeAssignment"/>s are added. Typical use: a player
    /// notifies the TD mid-round that they have to leave; the TD
    /// gives them the ½-pt bye so their opponent isn't left stranded.
    /// </summary>
    public static Tournament ConvertPairingToHalfPointBye(
        Tournament tournament,
        string sectionName,
        int round,
        int halfByePair)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        var targetRound = section.Rounds.FirstOrDefault(r => r.Number == round)
            ?? throw new InvalidOperationException(
                $"Round {round} does not exist in section '{sectionName}'.");

        var pairing = targetRound.Pairings.FirstOrDefault(
            p => p.WhitePair == halfByePair || p.BlackPair == halfByePair)
            ?? throw new InvalidOperationException(
                $"Pair #{halfByePair} is not assigned to a pairing in round {round}.");

        if (pairing.Result != PairingResult.Unplayed)
        {
            throw new InvalidOperationException(
                "Can't convert a scored pairing to a half-point bye; correct the result instead.");
        }

        var opponentPair = pairing.WhitePair == halfByePair
            ? pairing.BlackPair
            : pairing.WhitePair;

        // Drop the pairing and add two byes (½ for the target, 1 for opponent).
        var updatedPairings = targetRound.Pairings
            .Where(p => p != pairing)
            .ToArray();
        var updatedByes = targetRound.Byes
            .Append(new ByeAssignment(halfByePair,  ByeKind.Half))
            .Append(new ByeAssignment(opponentPair, ByeKind.Full))
            .ToArray();
        var updatedRound = targetRound with { Pairings = updatedPairings, Byes = updatedByes };

        var roundIndex = round - 1;
        var updatedPlayers = section.Players
            .Select(p => p.PairNumber == halfByePair
                    ? OverwriteHistoryAsBye(p, roundIndex, RoundResultKind.HalfPointBye, 0.5m)
                : p.PairNumber == opponentPair
                    ? OverwriteHistoryAsBye(p, roundIndex, RoundResultKind.FullPointBye, 1m)
                : p)
            .ToArray();

        var updatedSection = section with
        {
            Players = updatedPlayers,
            Rounds = section.Rounds.Select(r => r.Number == round ? updatedRound : r).ToArray(),
        };

        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    /// <summary>
    /// Converts a scheduled but un-played pairing into a late
    /// zero-point bye for <paramref name="zeroByePair"/>: that player
    /// is awarded 0 (SwissSys "U" kind), their opponent receives a
    /// full-point bye (1.0), and the pairing is removed from the
    /// round. Typical use: a player no-shows mid-round without
    /// advance notice and the TD wants to record the absence without
    /// giving them any tiebreak benefit. Differs from
    /// <see cref="ConvertPairingToHalfPointBye"/> only in the points
    /// and the RoundResultKind stamped on the target player's history.
    /// </summary>
    public static Tournament ConvertPairingToZeroPointBye(
        Tournament tournament,
        string sectionName,
        int round,
        int zeroByePair)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        var targetRound = section.Rounds.FirstOrDefault(r => r.Number == round)
            ?? throw new InvalidOperationException(
                $"Round {round} does not exist in section '{sectionName}'.");

        var pairing = targetRound.Pairings.FirstOrDefault(
            p => p.WhitePair == zeroByePair || p.BlackPair == zeroByePair)
            ?? throw new InvalidOperationException(
                $"Pair #{zeroByePair} is not assigned to a pairing in round {round}.");

        if (pairing.Result != PairingResult.Unplayed)
        {
            throw new InvalidOperationException(
                "Can't convert a scored pairing to a zero-point bye; correct the result instead.");
        }

        var opponentPair = pairing.WhitePair == zeroByePair
            ? pairing.BlackPair
            : pairing.WhitePair;

        var updatedPairings = targetRound.Pairings
            .Where(p => p != pairing)
            .ToArray();
        var updatedByes = targetRound.Byes
            .Append(new ByeAssignment(zeroByePair,   ByeKind.Unpaired))
            .Append(new ByeAssignment(opponentPair,  ByeKind.Full))
            .ToArray();
        var updatedRound = targetRound with { Pairings = updatedPairings, Byes = updatedByes };

        var roundIndex = round - 1;
        var updatedPlayers = section.Players
            .Select(p => p.PairNumber == zeroByePair
                    ? OverwriteHistoryAsBye(p, roundIndex, RoundResultKind.ZeroPointBye, 0m)
                : p.PairNumber == opponentPair
                    ? OverwriteHistoryAsBye(p, roundIndex, RoundResultKind.FullPointBye, 1m)
                : p)
            .ToArray();

        var updatedSection = section with
        {
            Players = updatedPlayers,
            Rounds = section.Rounds.Select(r => r.Number == round ? updatedRound : r).ToArray(),
        };

        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    private static Tournament UpdateSection(
        Tournament tournament,
        string sectionName,
        System.Func<Section, Section> transform)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        var section = FindSection(tournament, sectionName);
        var updated = transform(section);
        return ReferenceEquals(updated, section)
            ? tournament
            : ReplaceSection(tournament, sectionName, updated);
    }

    /// <summary>
    /// Appends the next round to a round-robin section using
    public static Tournament AppendRoundRobinRound(
        Tournament tournament,
        string sectionName)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        if (section.Kind != SectionKind.RoundRobin)
        {
            throw new InvalidOperationException(
                $"Section '{sectionName}' is not a round-robin (kind={section.Kind}).");
        }
        GuardNoSoftDeletedPlayers(section);

        // Build the full schedule in seed order. Withdrawn players are
        // excluded from the pool; their past history stays intact.
        var activeSeats = section.Players
            .Where(p => !p.Withdrawn)
            .OrderBy(p => p.PairNumber)
            .Select(p => p.PairNumber)
            .ToArray();

        var schedule = RoundRobinScheduler.Build(activeSeats);
        var nextRoundIndex = section.Rounds.Count;
        if (nextRoundIndex >= schedule.Count)
        {
            throw new InvalidOperationException(
                $"All {schedule.Count} scheduled rounds of '{sectionName}' have been paired.");
        }

        var scheduled = schedule[nextRoundIndex];
        var projected = new BbpPairingResult(
            Pairings: scheduled.Pairings
                .Select(p => new Bbp.BbpPairing(p.WhitePair, p.BlackPair))
                .ToArray(),
            ByePlayerPairs: scheduled.Byes
                .Where(b => b.Kind == ByeKind.Full)
                .Select(b => b.PlayerPair)
                .ToArray(),
            HalfPointByePlayerPairs: scheduled.Byes
                .Where(b => b.Kind == ByeKind.Half)
                .Select(b => b.PlayerPair)
                .ToArray());

        return AppendRound(tournament, sectionName, projected);
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

    /// <summary>
    /// Soft-deletes <paramref name="sectionName"/> by setting the
    /// <see cref="Section.SoftDeleted"/> flag. No data is discarded —
    /// the section's players, rounds, pairings, and results remain
    /// intact for later restoration via <see cref="UndeleteSection"/>.
    /// Once soft-deleted, every other section-targeted mutation
    /// (pair next round, set result, swap colours, ...) throws
    /// <see cref="InvalidOperationException"/> until the section is
    /// undeleted. The publishing pipeline
    /// (<c>SwissSysResultJsonBuilder</c>) also excludes soft-deleted
    /// sections from the uploaded results JSON.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The section does not exist, or is already soft-deleted.
    /// </exception>
    public static Tournament SoftDeleteSection(Tournament tournament, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSectionAllowSoftDeleted(tournament, sectionName);
        if (section.SoftDeleted)
        {
            throw new InvalidOperationException(
                $"Section '{sectionName}' is already soft-deleted.");
        }
        return ReplaceSection(tournament, sectionName, section with { SoftDeleted = true });
    }

    /// <summary>
    /// Clears the soft-deleted flag on <paramref name="sectionName"/>,
    /// restoring normal mutation + publishing behavior. No-op fails
    /// loudly so the UI can surface a sensible message.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The section does not exist, or is not currently soft-deleted.
    /// </exception>
    public static Tournament UndeleteSection(Tournament tournament, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSectionAllowSoftDeleted(tournament, sectionName);
        if (!section.SoftDeleted)
        {
            throw new InvalidOperationException(
                $"Section '{sectionName}' is not soft-deleted.");
        }
        return ReplaceSection(tournament, sectionName, section with { SoftDeleted = false });
    }

    /// <summary>
    /// Permanently removes <paramref name="sectionName"/> from the
    /// tournament. All players, rounds, pairings, results, prizes,
    /// etc. belonging to the section are discarded. The next save
    /// propagates the removal to the underlying <c>.sjson</c>
    /// (<see cref="SwissSysTournamentWriter"/> prunes any section
    /// nodes present in the raw file but absent from the domain
    /// model). Unlike soft-delete this cannot be undone short of
    /// restoring a backup.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The section does not exist.
    /// </exception>
    public static Tournament HardDeleteSection(Tournament tournament, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        // Throws if missing, but ignores soft-deleted state — hard
        // delete applies to both live and soft-deleted sections.
        _ = FindSectionAllowSoftDeleted(tournament, sectionName);

        var remaining = tournament.Sections
            .Where(s => s.Name != sectionName)
            .ToArray();
        return tournament with { Sections = remaining };
    }

    // ================================================================
    // Player lifecycle: soft-delete / undelete / hard-delete
    // ================================================================

    /// <summary>
    /// Soft-deletes a player. Only permitted before any round of the
    /// section is paired; the mutations layer rejects the toggle once
    /// <see cref="Section.RoundsPaired"/> is positive (use
    /// <see cref="SetPlayerWithdrawn"/> instead). Soft-deleted players
    /// are excluded from standings, wall chart, TRF export, publishing,
    /// and BBP pairing input; no data is discarded so
    /// <see cref="UndeletePlayer"/> restores them cleanly.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The section or player does not exist, the section already has
    /// at least one round paired, or the player is already soft-deleted.
    /// </exception>
    public static Tournament SoftDeletePlayer(
        Tournament tournament, string sectionName, int pairNumber)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        GuardPreRoundOneForPlayerDelete(section, nameof(SoftDeletePlayer));

        var player = FindPlayer(section, pairNumber);
        if (player.SoftDeleted)
        {
            throw new InvalidOperationException(
                $"Player #{pairNumber} in section '{sectionName}' is already soft-deleted.");
        }

        var updated = player with { SoftDeleted = true };
        return ReplacePlayer(tournament, section, updated);
    }

    /// <summary>
    /// Clears the soft-deleted flag on a player. Allowed regardless of
    /// <see cref="Section.RoundsPaired"/> so the TD can recover a
    /// section where someone soft-deleted a player and then accidentally
    /// paired a round (which should be blocked by the
    /// <c>GuardNoSoftDeletedPlayers</c> check, but belt-and-braces).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The section or player does not exist, or the player is not
    /// currently soft-deleted.
    /// </exception>
    public static Tournament UndeletePlayer(
        Tournament tournament, string sectionName, int pairNumber)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        var player = FindPlayer(section, pairNumber);
        if (!player.SoftDeleted)
        {
            throw new InvalidOperationException(
                $"Player #{pairNumber} in section '{sectionName}' is not soft-deleted.");
        }

        var updated = player with { SoftDeleted = false };
        return ReplacePlayer(tournament, section, updated);
    }

    /// <summary>
    /// Permanently removes a player from a section. Only permitted
    /// before any round of the section is paired — once pairings exist
    /// the player's history is entangled with their opponents' and
    /// removing them would corrupt tiebreaks. After round 1 pairing,
    /// use <see cref="SetPlayerWithdrawn"/> instead. The next save
    /// propagates the removal to the raw <c>.sjson</c>
    /// (<see cref="SwissSysTournamentWriter"/> prunes player nodes
    /// missing from the domain model). Works on both live and
    /// soft-deleted players.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The section or player does not exist, or the section already
    /// has at least one round paired.
    /// </exception>
    public static Tournament HardDeletePlayer(
        Tournament tournament, string sectionName, int pairNumber)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = FindSection(tournament, sectionName);
        GuardPreRoundOneForPlayerDelete(section, nameof(HardDeletePlayer));
        _ = FindPlayer(section, pairNumber);

        var remaining = section.Players
            .Where(p => p.PairNumber != pairNumber)
            .ToArray();
        var updatedSection = section with { Players = remaining };
        return ReplaceSection(tournament, sectionName, updatedSection);
    }

    // ================================================================
    // Helpers
    // ================================================================

    private static Player FindPlayer(Section section, int pairNumber) =>
        section.Players.FirstOrDefault(p => p.PairNumber == pairNumber)
        ?? throw new InvalidOperationException(
            $"Player #{pairNumber} not found in section '{section.Name}'.");

    private static Tournament ReplacePlayer(Tournament t, Section section, Player replacement)
    {
        var players = section.Players
            .Select(p => p.PairNumber == replacement.PairNumber ? replacement : p)
            .ToArray();
        var updated = section with { Players = players };
        return ReplaceSection(t, section.Name, updated);
    }

    private static void GuardPreRoundOneForPlayerDelete(Section section, string op)
    {
        if (section.RoundsPaired > 0)
        {
            throw new InvalidOperationException(
                $"{op} is only allowed before round 1 is paired. " +
                $"Section '{section.Name}' already has {section.RoundsPaired} round(s) paired; " +
                $"use SetPlayerWithdrawn instead.");
        }
    }

    /// <summary>
    /// Pair-round-N defense-in-depth check. The UI also prompts the TD
    /// to resolve soft-deleted players before pairing a round, but the
    /// mutation refuses too so scripted / batch callers can't sidestep
    /// the guard.
    /// </summary>
    private static void GuardNoSoftDeletedPlayers(Section section)
    {
        if (section.Players.Any(p => p.SoftDeleted))
        {
            var names = string.Join(", ",
                section.Players.Where(p => p.SoftDeleted).Select(p => $"#{p.PairNumber} {p.Name}"));
            throw new InvalidOperationException(
                $"Section '{section.Name}' has soft-deleted players ({names}). " +
                $"Restore or permanently delete them before pairing a round.");
        }
    }

    private static Section FindSection(Tournament t, string name)
    {
        var section = FindSectionAllowSoftDeleted(t, name);
        if (section.SoftDeleted)
        {
            throw new InvalidOperationException(
                $"Section '{name}' is soft-deleted. Undelete it before making changes.");
        }
        return section;
    }

    /// <summary>
    /// Variant of <see cref="FindSection"/> that returns soft-deleted
    /// sections too. Only the soft-delete / undelete / hard-delete
    /// mutations should use this; every other section-targeted
    /// mutation must go through <see cref="FindSection"/> so the
    /// soft-deleted guard fires.
    /// </summary>
    private static Section FindSectionAllowSoftDeleted(Tournament t, string name) =>
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

    // ---- helpers for SwapPairingColors / SwapBoardOpponents / ConvertPairingToHalfPointBye ----

    /// <summary>
    /// Returns <paramref name="player"/> with the history entry at
    /// <paramref name="roundIndex"/> mirrored to the opposite colour.
    /// Opponent / board / scoring fields stay as-is.
    /// </summary>
    private static Player FlipHistoryColor(Player player, int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= player.History.Count) return player;
        var h = player.History[roundIndex];
        var flipped = h.Color switch
        {
            PlayerColor.White => PlayerColor.Black,
            PlayerColor.Black => PlayerColor.White,
            _ => h.Color,
        };
        var updated = player.History.ToArray();
        updated[roundIndex] = h with { Color = flipped };
        return player with { History = updated };
    }

    /// <summary>
    /// Overwrites <paramref name="player"/>'s history entry at
    /// <paramref name="roundIndex"/> with a fresh un-played pairing
    /// cell (Kind=None with the given opponent / colour / board).
    /// Scoring fields reset to zero since the game hasn't been played.
    /// </summary>
    private static Player OverwriteHistoryPairing(
        Player player,
        int roundIndex,
        int opponent,
        PlayerColor color,
        int board)
    {
        if (roundIndex < 0 || roundIndex >= player.History.Count) return player;
        var updated = player.History.ToArray();
        updated[roundIndex] = new RoundResult(
            Kind: RoundResultKind.None,
            Opponent: opponent,
            Color: color,
            Board: board,
            Logic1: 0,
            Logic2: 0,
            GamePoints: 0m);
        return player with { History = updated };
    }

    /// <summary>
    /// Overwrites the history entry at <paramref name="roundIndex"/>
    /// with a bye cell (Full or Half). Used when a pairing is
    /// converted mid-round.
    /// </summary>
    private static Player OverwriteHistoryAsBye(
        Player player,
        int roundIndex,
        RoundResultKind kind,
        decimal score)
    {
        if (roundIndex < 0 || roundIndex >= player.History.Count) return player;
        var updated = player.History.ToArray();
        updated[roundIndex] = new RoundResult(
            Kind: kind,
            Opponent: -1,
            Color: PlayerColor.None,
            Board: 0,
            Logic1: 0,
            Logic2: 0,
            GamePoints: score);
        return player with { History = updated };
    }

    /// <summary>
    /// Returns true when <paramref name="a"/> and <paramref name="b"/>
    /// have already played each other in a round strictly before
    /// <paramref name="excludedRoundIndex"/>. Bye entries and
    /// unpaired slots are ignored.
    /// </summary>
    private static bool HasPlayedBefore(
        IReadOnlyDictionary<int, Player> byPair,
        int a,
        int b,
        int excludedRoundIndex)
    {
        if (!byPair.TryGetValue(a, out var pa)) return false;
        for (var i = 0; i < pa.History.Count; i++)
        {
            if (i == excludedRoundIndex) continue;
            if (pa.History[i].Opponent == b) return true;
        }
        return false;
    }
}
