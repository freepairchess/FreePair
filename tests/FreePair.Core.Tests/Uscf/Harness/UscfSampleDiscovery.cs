using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreePair.Core.Tests.Uscf.Harness;

/// <summary>
/// Walks <see cref="TestPaths.UscfSampleRoot"/> and yields every
/// SwissSys-state file the USCF verification harness should look at.
/// </summary>
/// <remarks>
/// <para>The corpus is described in <c>docs/samples/swisssys/uscf/README.md</c>.
/// Each tournament directory typically contains:</para>
/// <list type="bullet">
///   <item>Exactly one final-state SwissSys file (<c>.sjson</c> in older
///         events, <c>.json</c> in 11.34+).</item>
///   <item>USCF rating-report DBFs alongside (not used by this harness).</item>
///   <item>A <c>Backups/</c> folder of <c>.BK</c> snapshots — these are
///         renamed <c>.sjson</c> files that the per-round harness (P5b)
///         will use; the round-1 harness ignores them.</item>
/// </list>
/// <para>The discovery is permissive: missing-corpus is treated as zero
/// cases (so a fresh dev clone without the samples doesn't fail tests),
/// not an error.</para>
/// </remarks>
internal static class UscfSampleDiscovery
{
    /// <summary>
    /// Top-level final-state files (one per tournament). Skips anything
    /// inside <c>Backups/</c> or <c>SwissManager/</c> sub-folders.
    /// </summary>
    public static IReadOnlyList<string> FinalStateFiles()
    {
        var root = TestPaths.UscfSampleRoot;
        if (!Directory.Exists(root))
        {
            return System.Array.Empty<string>();
        }

        // Final-state files live one level deep under uscf/<event>/
        // — never inside a subfolder. This rule keeps Backups/*.BK out
        // of the round-1 corpus while preserving them for P5b.
        var files = new List<string>();
        foreach (var eventDir in Directory.EnumerateDirectories(root))
        {
            foreach (var ext in new[] { "*.sjson", "*.json" })
            {
                files.AddRange(Directory.EnumerateFiles(eventDir, ext, SearchOption.TopDirectoryOnly));
            }
        }

        return files
            .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
