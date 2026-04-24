using System;
using System.Globalization;

namespace FreePair.Core.SwissSys;

/// <summary>
/// Strongly-typed representation of a single SwissSys <c>.sjson</c> player
/// history token. The on-disk format is a semicolon-separated 7-tuple:
/// <c>&lt;Result&gt;;&lt;Opponent&gt;;&lt;Color&gt;;&lt;Board&gt;;&lt;Logic1&gt;;&lt;Logic2&gt;;&lt;GamePoints&gt;</c>.
/// </summary>
/// <remarks>
/// <para>Result prefixes:
/// <list type="bullet">
///   <item><c>+</c> win</item>
///   <item><c>-</c> loss</item>
///   <item><c>=</c> draw</item>
///   <item><c>H</c> half-point bye</item>
///   <item><c>B</c> full-point bye</item>
///   <item><c>~</c> not paired</item>
///   <item><c>\u0000</c> or empty — uninitialized slot</item>
/// </list></para>
/// <para><see cref="Logic1"/>, <see cref="Logic2"/> and <see cref="GamePoints"/>
/// are SwissSys pairing-engine internals preserved here only for round-trip
/// fidelity; consumers should prefer the parsed kind/opponent/color/board
/// fields for display and scoring.</para>
/// </remarks>
public readonly record struct RoundResult(
    RoundResultKind Kind,
    int Opponent,
    PlayerColor Color,
    int Board,
    int Logic1,
    int Logic2,
    decimal GamePoints)
{
    /// <summary>
    /// An uninitialized round slot, equivalent to a raw <c>\u0000;0;-;0;0;0;0</c>
    /// token.
    /// </summary>
    public static RoundResult Empty { get; } =
        new(RoundResultKind.None, 0, PlayerColor.None, 0, 0, 0, 0m);

    /// <summary>
    /// True when this slot represents no game (either unpaired or
    /// uninitialized).
    /// </summary>
    public bool IsUnplayed =>
        Kind == RoundResultKind.None;

    /// <summary>
    /// True when this slot represents a bye of either kind.
    /// </summary>
    public bool IsBye =>
        Kind is RoundResultKind.FullPointBye
             or RoundResultKind.HalfPointBye
             or RoundResultKind.ZeroPointBye;

    /// <summary>
    /// Game points awarded for this result (1 / 0.5 / 0). Does <b>not</b>
    /// consult <see cref="GamePoints"/>, which is a SwissSys-internal value
    /// that is usually zero.
    /// </summary>
    public decimal Score => Kind switch
    {
        RoundResultKind.Win => 1m,
        RoundResultKind.FullPointBye => 1m,
        RoundResultKind.Draw => 0.5m,
        RoundResultKind.HalfPointBye => 0.5m,
        _ => 0m,
    };

    /// <summary>
    /// Parses a SwissSys result token. Throws <see cref="FormatException"/>
    /// when the token cannot be interpreted.
    /// </summary>
    public static RoundResult Parse(string token)
    {
        if (!TryParse(token, out var result, out var error))
        {
            throw new FormatException(error);
        }

        return result;
    }

    /// <summary>
    /// Attempts to parse a SwissSys result token. Returns <c>false</c> and
    /// yields <see cref="Empty"/> when the token is malformed.
    /// </summary>
    public static bool TryParse(string? token, out RoundResult result)
        => TryParse(token, out result, out _);

    /// <summary>
    /// Serializes this result back to the SwissSys 7-tuple token format
    /// (<c>&lt;prefix&gt;;&lt;opp&gt;;&lt;color&gt;;&lt;board&gt;;&lt;logic1&gt;;&lt;logic2&gt;;&lt;gamepoints&gt;</c>).
    /// Paired-but-unplayed rounds (<see cref="RoundResultKind.None"/> with a
    /// positive opponent) are encoded with a <c>~</c> prefix so the pairing
    /// survives a round-trip through FreePair.
    /// </summary>
    public string ToSwissSysToken()
    {
        var prefix = Kind switch
        {
            RoundResultKind.Win          => "+",
            RoundResultKind.Loss         => "-",
            RoundResultKind.Draw         => "=",
            RoundResultKind.FullPointBye => "B",
            RoundResultKind.HalfPointBye => "H",
            RoundResultKind.ZeroPointBye => "U",
            _                            => "~",
        };

        var color = Color switch
        {
            PlayerColor.White => "W",
            PlayerColor.Black => "B",
            _                 => "-",
        };

        var gp = GamePoints.ToString("0.##", CultureInfo.InvariantCulture);

        return string.Create(CultureInfo.InvariantCulture,
            $"{prefix};{Opponent};{color};{Board};{Logic1};{Logic2};{gp}");
    }

    private static bool TryParse(string? token, out RoundResult result, out string? error)
    {
        result = Empty;
        error = null;

        if (token is null)
        {
            error = "Result token was null.";
            return false;
        }

        // Normalize the SwissSys "uninitialized" marker: the JSON literal is
        // a single NUL character, sometimes followed by the usual tail
        // (";0;-;0;0;0;0"). Treat a leading NUL as the None prefix.
        var parts = token.Split(';');

        var kindToken = parts.Length > 0 ? parts[0] : string.Empty;
        if (!TryParseKind(kindToken, out var kind))
        {
            error = $"Unrecognized result prefix '{kindToken}'.";
            return false;
        }

        var opponent = ParseIntOrDefault(parts, 1, 0);
        var color = ParseColor(parts, 2);
        var board = ParseIntOrDefault(parts, 3, 0);
        var logic1 = ParseIntOrDefault(parts, 4, 0);
        var logic2 = ParseIntOrDefault(parts, 5, 0);
        var gamePoints = ParseDecimalOrDefault(parts, 6, 0m);

        result = new RoundResult(kind, opponent, color, board, logic1, logic2, gamePoints);
        return true;
    }

    private static bool TryParseKind(string token, out RoundResultKind kind)
    {
        // Empty string and leading-NUL both mean "uninitialized".
        if (token.Length == 0 || token[0] == '\0')
        {
            kind = RoundResultKind.None;
            return true;
        }

        switch (token)
        {
            case "+": kind = RoundResultKind.Win; return true;
            case "-": kind = RoundResultKind.Loss; return true;
            case "=": kind = RoundResultKind.Draw; return true;
            case "H": kind = RoundResultKind.HalfPointBye; return true;
            case "B": kind = RoundResultKind.FullPointBye; return true;
            case "~": kind = RoundResultKind.None; return true;
            // SwissSys "U" = player was unpaired for this round and
            // received no points. Distinct from "~" / empty: "U" is
            // an affirmative "got a zero-point bye" mark from the TD
            // or SwissSys itself (late entry, withdrawal round, etc.).
            // Modelled as RoundResultKind.ZeroPointBye with Score == 0.
            case "U": kind = RoundResultKind.ZeroPointBye; return true;
            // Forfeit wins / losses. USCF rules: scoring is identical to
            // a regular win / loss. We don't preserve the forfeit-vs-
            // played distinction in the domain model (v1), so downstream
            // display will show "W" / "L" instead of "X" / "F".
            case "X": kind = RoundResultKind.Win;  return true;
            case "F": kind = RoundResultKind.Loss; return true;
            default:
                kind = RoundResultKind.None;
                return false;
        }
    }

    private static PlayerColor ParseColor(string[] parts, int index)
    {
        if (index >= parts.Length)
        {
            return PlayerColor.None;
        }

        return parts[index] switch
        {
            "W" => PlayerColor.White,
            "B" => PlayerColor.Black,
            _ => PlayerColor.None,
        };
    }

    private static int ParseIntOrDefault(string[] parts, int index, int fallback)
    {
        if (index >= parts.Length)
        {
            return fallback;
        }

        return int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static decimal ParseDecimalOrDefault(string[] parts, int index, decimal fallback)
    {
        if (index >= parts.Length)
        {
            return fallback;
        }

        return decimal.TryParse(parts[index], NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }
}
