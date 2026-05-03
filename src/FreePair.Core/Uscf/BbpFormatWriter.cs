using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FreePair.Core.Uscf;

/// <summary>
/// Writes <see cref="UscfPairingResult"/> in the BBP plain-text pairings
/// format that <c>FreePair.Core.Bbp.BbpPairingsParser</c> consumes:
/// <code>
/// {totalLines}
/// {white_pair} {black_pair}
/// ...
/// {bye_pair} 0
/// </code>
/// where each line represents one board (or one bye, with <c>0</c> in the
/// black-pair column).
/// </summary>
public static class BbpFormatWriter
{
    /// <summary>Writes the result to <paramref name="writer"/>.</summary>
    public static void Write(UscfPairingResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var byeCount = result.ByePair is null ? 0 : 1;
        var total = result.Pairings.Count + byeCount;

        writer.WriteLine(total.ToString(CultureInfo.InvariantCulture));
        foreach (var p in result.Pairings)
        {
            writer.Write(p.WhitePair.ToString(CultureInfo.InvariantCulture));
            writer.Write(' ');
            writer.WriteLine(p.BlackPair.ToString(CultureInfo.InvariantCulture));
        }
        if (result.ByePair is int bye)
        {
            writer.Write(bye.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine(" 0");
        }
    }

    /// <summary>Convenience: render to a string.</summary>
    public static string Write(UscfPairingResult result)
    {
        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        sw.NewLine = "\n";
        Write(result, sw);
        return sw.ToString();
    }
}
