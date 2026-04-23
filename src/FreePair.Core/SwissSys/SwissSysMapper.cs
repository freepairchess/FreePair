using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FreePair.Core.SwissSys.Raw;
using FreePair.Core.Tournaments;

namespace FreePair.Core.SwissSys;

/// <summary>
/// Pure, stateless mapper that translates a verbatim
/// <see cref="RawSwissSysDocument"/> into the clean FreePair domain model
/// rooted at <see cref="Tournament"/>.
/// </summary>
public static class SwissSysMapper
{
    /// <summary>
    /// Maps a raw SwissSys document into a domain-level <see cref="Tournament"/>.
    /// </summary>
    public static Tournament Map(RawSwissSysDocument raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var sections = raw.Sections
            .Select(MapSection)
            .ToArray();

        return new Tournament(
            Title: raw.Overview?.TournamentTitle,
            StartDate: ParseIsoDate(raw.Overview?.StartingDate),
            EndDate: ParseIsoDate(raw.Overview?.EndingDate),
            TimeControl: raw.Overview?.TournamentTimeControls,
            NachEventId: raw.Overview?.NachEventId,
            Sections: sections,

            OrganizerId:     raw.Overview?.OrganizerId,
            OrganizerIdType: raw.Overview?.OrganizerIdType,
            OrganizerName:   raw.Overview?.OrganizerName,
            NachPasscode:    raw.Overview?.NachPasscode,
            StartDateTime:   ParseIsoDateTime(raw.Overview?.StartingDateTime),
            EndDateTime:     ParseIsoDateTime(raw.Overview?.EndingDateTime),
            TimeZone:        raw.Overview?.TimeZone,

            EventAddress: raw.Overview?.EventAddress,
            EventCity:    raw.Overview?.EventCity,
            EventState:   raw.Overview?.EventState,
            EventZipCode: raw.Overview?.EventZipCode?.Trim(),
            EventCountry: raw.Overview?.EventCountry,

            EventFormat:     raw.Overview?.EventFormat,
            EventType:       raw.Overview?.EventType,
            PairingRule:     raw.Overview?.PairingRule,
            TimeControlType: raw.Overview?.TimeControlType,
            RatingType:      raw.Overview?.RatingType,

            RoundsPlanned:        raw.Overview?.Rounds,
            HalfPointByesAllowed: raw.Overview?.HalfPointByes,
            AutoPublishPairings:  raw.Overview?.FreePairAutoPublishPairings,
            AutoPublishResults:   raw.Overview?.FreePairAutoPublishResults,
            LastPublishedAt:      ParseIsoTimestamp(raw.Overview?.FreePairLastPublishedAt));
    }

    /// <summary>
    /// Parses an ISO-8601 timestamp safely. Returns <see langword="null"/>
    /// on null / whitespace / invalid input so a corrupt or hand-edited
    /// Overview block doesn't crash tournament load.
    /// </summary>
    private static System.DateTimeOffset? ParseIsoTimestamp(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return System.DateTimeOffset.TryParse(
            s,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var t) ? t : null;
    }

    /// <summary>
    /// Parses a best-effort ISO 8601 timestamp. Returns null on any
    /// failure rather than throwing — the writer's raw-JSON pass-through
    /// will preserve the original text even if we couldn't understand it.
    /// </summary>
    private static DateTimeOffset? ParseIsoDateTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out var dto))
        {
            return dto;
        }
        return null;
    }

    internal static Section MapSection(RawSection raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var players = raw.Players.Select(MapPlayer).ToArray();
        var teams = raw.Teams.Select(MapTeam).ToArray();
        var rounds = ReconstructRounds(players, Math.Max(raw.RoundsPaired, raw.RoundsPlayed));
        var prizes = MapPrizes(raw.Prizes);

        return new Section(
            Name: raw.SectionName ?? string.Empty,
            Title: raw.SectionTitle,
            Kind: MapKind(raw.Type),
            TimeControl: raw.SectionTimeControl,
            RoundsPaired: raw.RoundsPaired,
            RoundsPlayed: raw.RoundsPlayed,
            FinalRound: raw.FinalRound,
            FirstBoard: raw.FirstBoard,
            Players: players,
            Teams: teams,
            Rounds: rounds,
            Prizes: prizes,
            UseAcceleration: raw.Acceleration != 0,
            InitialColor: MapCoinToss(raw.CoinToss),
            SoftDeleted: raw.FreePairSoftDeleted ?? false);
    }

    /// <summary>
    /// Maps SwissSys's per-section <c>Coin toss</c> field to our
    /// <see cref="Bbp.InitialColor"/> enum. SwissSys convention:
    /// 0 = board-1 white, 1 = board-1 black; anything else defaults
    /// to white to match bbpPairings' own default.
    /// </summary>
    internal static Bbp.InitialColor MapCoinToss(int coinToss) => coinToss switch
    {
        1 => Bbp.InitialColor.Black,
        _ => Bbp.InitialColor.White,
    };

    internal static Player MapPlayer(RawPlayer raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var history = raw.Results
            .Select(RoundResult.Parse)
            .ToArray();

        return new Player(
            PairNumber: raw.PairNumber,
            Name: raw.Name ?? string.Empty,
            UscfId: raw.Id,
            Rating: raw.Rating,
            SecondaryRating: raw.Rating2,
            MembershipExpiration: raw.MembershipExpiration,
            Club: raw.Club,
            State: raw.State,
            Team: raw.Team,
            RequestedByeRounds: ParseReservedByes(raw.ReservedByes),
            History: history,
            Email: raw.Email,
            Phone: raw.Phone);
    }

    internal static Team MapTeam(RawTeam raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var history = raw.Results
            .Select(RoundResult.Parse)
            .ToArray();

        return new Team(
            PairNumber: raw.PairNumber,
            Name: raw.FullName ?? string.Empty,
            Code: raw.TeamCode,
            Rating: raw.Rating,
            History: history);
    }

    internal static Prizes MapPrizes(RawPrizes? raw)
    {
        if (raw is null)
        {
            return Prizes.Empty;
        }

        var place = raw.PlacePrizes
            .Select(p => new Prize(p.Value, p.Description))
            .ToArray();

        var @class = raw.ClassPrizes
            .Select(p => new Prize(p.Value, p.Description))
            .ToArray();

        return new Prizes(place, @class);
    }

    internal static SectionKind MapKind(int type) => type switch
    {
        0 => SectionKind.Swiss,
        1 => SectionKind.RoundRobin,
        _ => SectionKind.Unknown,
    };

    /// <summary>
    /// Parses the SwissSys <c>Reserved byes</c> field (a whitespace-separated
    /// list of round numbers such as <c>"1 "</c> or <c>" 1 3"</c>) into a
    /// sorted, de-duplicated list of round numbers.
    /// </summary>
    internal static IReadOnlyList<int> ParseReservedByes(string? reservedByes)
    {
        if (string.IsNullOrWhiteSpace(reservedByes))
        {
            return Array.Empty<int>();
        }

        var rounds = new SortedSet<int>();
        var tokens = reservedByes.Split(
            new[] { ' ', '\t', ',', ';' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var round) &&
                round > 0)
            {
                rounds.Add(round);
            }
        }

        return rounds.Count == 0 ? Array.Empty<int>() : rounds.ToArray();
    }

    /// <summary>
    /// Rebuilds per-round <see cref="Round"/> objects from the semicolon-
    /// separated per-player histories stored in a SwissSys <c>.sjson</c> file.
    /// </summary>
    internal static IReadOnlyList<Round> ReconstructRounds(
        IReadOnlyList<Player> players,
        int roundsPlayed)
    {
        if (players.Count == 0 || roundsPlayed <= 0)
        {
            return Array.Empty<Round>();
        }

        var rounds = new List<Round>(roundsPlayed);

        for (var r = 0; r < roundsPlayed; r++)
        {
            var pairings = new List<Pairing>();
            var byes = new List<ByeAssignment>();
            var seen = new HashSet<(int Lo, int Hi)>();

            foreach (var player in players)
            {
                if (r >= player.History.Count)
                {
                    continue;
                }

                var res = player.History[r];

                switch (res.Kind)
                {
                    case RoundResultKind.FullPointBye:
                        byes.Add(new ByeAssignment(player.PairNumber, ByeKind.Full));
                        break;

                    case RoundResultKind.HalfPointBye:
                        byes.Add(new ByeAssignment(player.PairNumber, ByeKind.Half));
                        break;

                    case RoundResultKind.None:
                        // Three sub-cases share the "None" kind:
                        //   1. Paired but not yet played — set by FreePair
                        //      after BBP output. Opponent > 0 and a real
                        //      colour is present; we re-emit as an
                        //      Unplayed pairing.
                        //   2. True unpaired (withdrawal, odd-count sit-out)
                        //      — opponent 0 / missing colour.
                        //   3. Uninitialized future slot — we already skip
                        //      these via the roundsPlayed bound.
                        if (res.Opponent > 0 && res.Color != PlayerColor.None)
                        {
                            TryAddPairing(player, res, seen, pairings);
                        }
                        else
                        {
                            byes.Add(new ByeAssignment(player.PairNumber, ByeKind.Unpaired));
                        }
                        break;

                    case RoundResultKind.Win:
                    case RoundResultKind.Loss:
                    case RoundResultKind.Draw:
                        if (!TryAddPairing(player, res, seen, pairings))
                        {
                            // Duplicate — already emitted from the other side.
                        }

                        break;
                }
            }

            var ordered = pairings
                .OrderBy(p => p.Board)
                .ToArray();

            rounds.Add(new Round(r + 1, ordered, byes.ToArray()));
        }

        return rounds;
    }

    private static bool TryAddPairing(
        Player player,
        RoundResult res,
        HashSet<(int Lo, int Hi)> seen,
        List<Pairing> pairings)
    {
        if (res.Opponent <= 0)
        {
            return false;
        }

        var key = player.PairNumber < res.Opponent
            ? (player.PairNumber, res.Opponent)
            : (res.Opponent, player.PairNumber);

        if (!seen.Add(key))
        {
            return false;
        }

        int white, black;
        PairingResult result;

        if (res.Color == PlayerColor.White)
        {
            white = player.PairNumber;
            black = res.Opponent;
            result = res.Kind switch
            {
                RoundResultKind.Win => PairingResult.WhiteWins,
                RoundResultKind.Loss => PairingResult.BlackWins,
                RoundResultKind.Draw => PairingResult.Draw,
                _ => PairingResult.Unplayed,
            };
        }
        else
        {
            // Treat any non-White color (Black or missing) as Black-side entry.
            white = res.Opponent;
            black = player.PairNumber;
            result = res.Kind switch
            {
                RoundResultKind.Win => PairingResult.BlackWins,
                RoundResultKind.Loss => PairingResult.WhiteWins,
                RoundResultKind.Draw => PairingResult.Draw,
                _ => PairingResult.Unplayed,
            };
        }

        pairings.Add(new Pairing(res.Board, white, black, result));
        return true;
    }

    private static DateOnly? ParseIsoDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date;
        }

        return DateOnly.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out date)
            ? date
            : null;
    }
}
