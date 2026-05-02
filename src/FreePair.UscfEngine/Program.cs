using System;
using System.IO;
using System.Text;
using FreePair.Core.Uscf;
using FreePair.Core.Uscf.Trf;

namespace FreePair.UscfEngine;

/// <summary>
/// FreePair's USCF Swiss pairing engine, packaged as a thin console wrapper
/// around <see cref="UscfPairer"/> so it can be dropped in as a binary
/// replacement for <c>bbpPairings.exe</c>.
/// </summary>
/// <remarks>
/// <para>The CLI surface mirrors <c>bbpPairings</c> deliberately so existing
/// FreePair installs can swap engines by changing only
/// <c>Settings.PairingEngineBinaryPath</c>:</para>
/// <code>
/// FreePair.UscfEngine.exe [--dutch | --uscf] [--baku] &lt;input.trf&gt; -p &lt;output.txt&gt;
/// </code>
/// <para>Recognised flags:</para>
/// <list type="bullet">
///   <item><c>--dutch</c> — accepted for FreePair / bbpPairings compatibility.
///         FreePair always passes this; we treat it as a synonym for
///         <c>--uscf</c> since this binary only knows USCF rules.</item>
///   <item><c>--uscf</c> — explicit opt-in; same effect as <c>--dutch</c>.</item>
///   <item><c>--baku</c> — accepted but currently unimplemented (USCF rarely
///         uses Baku acceleration; SwissSys uses a different scheme). A
///         warning is written to stderr and pairing proceeds without
///         acceleration.</item>
///   <item><c>-p &lt;file&gt;</c> — required output path for the BBP-format
///         pairings file.</item>
///   <item>positional arg — the input TRF file path.</item>
/// </list>
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (!TryParseArgs(args, out var input, out var output, out var error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine();
                Console.Error.WriteLine(UsageText);
                return 2;
            }

            var trfText = File.ReadAllText(input!, Encoding.ASCII);
            var doc = TrfReader.Parse(trfText);
            var result = UscfPairer.Pair(doc);

            using var writer = new StreamWriter(output!, append: false, Encoding.ASCII);
            BbpFormatWriter.Write(result, writer);
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Input file not found: {ex.FileName}");
            return 1;
        }
        catch (Exception ex)
        {
            // Mirrors bbpPairings' behaviour: nonzero exit + diagnostic on
            // stderr. The FreePair host preserves the failed TRF for
            // inspection.
            Console.Error.WriteLine($"FreePair.UscfEngine: {ex.Message}");
            return 1;
        }
    }

    private static bool TryParseArgs(
        string[] args,
        out string? input,
        out string? output,
        out string error)
    {
        input = null;
        output = null;
        error = string.Empty;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--dutch":
                case "--uscf":
                    // Mode flags — currently a no-op because USCF is the
                    // only mode this binary knows. Accepted for CLI
                    // compatibility with bbpPairings.
                    break;
                case "--baku":
                    Console.Error.WriteLine(
                        "warning: --baku acceleration is not yet implemented in FreePair.UscfEngine; " +
                        "proceeding without acceleration.");
                    break;
                case "-p":
                    if (i + 1 >= args.Length)
                    {
                        error = "Missing argument after -p.";
                        return false;
                    }
                    output = args[++i];
                    break;
                default:
                    if (a.StartsWith('-'))
                    {
                        error = $"Unrecognised flag: {a}";
                        return false;
                    }
                    if (input is not null)
                    {
                        error = $"Unexpected positional argument: {a} (input already set to {input}).";
                        return false;
                    }
                    input = a;
                    break;
            }
        }

        if (string.IsNullOrEmpty(input))
        {
            error = "Missing input TRF path.";
            return false;
        }
        if (string.IsNullOrEmpty(output))
        {
            error = "Missing -p <output.txt>.";
            return false;
        }
        return true;
    }

    private const string UsageText =
        "Usage: FreePair.UscfEngine [--dutch|--uscf] [--baku] <input.trf> -p <output.txt>";
}
