using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FreePair.Core.Bbp;

/// <summary>
/// Parses the plain-text pairings output produced by <c>bbpPairings -p</c>.
/// </summary>
/// <remarks>
/// <para>The BBP plain-text format is intentionally minimal:</para>
/// <code>
/// N
/// whitePair1 blackPair1
/// whitePair2 blackPair2
/// ...
/// </code>
/// <para>A value of <c>0</c> in either column represents a bye. The leading
/// count line is optional — some BBP variants omit it — so the parser is
/// tolerant of either layout.</para>
/// </remarks>
public static class BbpPairingsParser
{
    /// <summary>Parses the full text of a BBP pairings file.</summary>
    public static BbpPairingResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lines = text
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        if (lines.Length == 0)
        {
            return new BbpPairingResult(Array.Empty<BbpPairing>(), Array.Empty<int>());
        }

        var startIndex = 0;
        if (IsSingleInteger(lines[0]))
        {
            // First line is the pairing count header — skip it.
            startIndex = 1;
        }

        var pairings = new List<BbpPairing>();
        var byes = new List<int>();

        for (var i = startIndex; i < lines.Length; i++)
        {
            var parts = lines[i].Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2) continue;

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
            {
                continue;
            }

            if (a == 0 && b == 0)
            {
                continue;
            }

            if (a == 0)
            {
                byes.Add(b);
            }
            else if (b == 0)
            {
                byes.Add(a);
            }
            else
            {
                pairings.Add(new BbpPairing(a, b));
            }
        }

        return new BbpPairingResult(pairings, byes);
    }

    private static bool IsSingleInteger(string line) =>
        !line.Contains(' ') &&
        !line.Contains('\t') &&
        int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
}
