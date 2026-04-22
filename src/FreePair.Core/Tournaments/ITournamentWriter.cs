using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Persists an in-memory <see cref="Tournament"/> back to its source file.
/// </summary>
public interface ITournamentWriter
{
    /// <summary>
    /// Writes the given <paramref name="tournament"/> to
    /// <paramref name="filePath"/>, preserving unmodelled fields in the
    /// source file (e.g. seating, prizes, raw pair-table rows). Intended to
    /// be invoked after every TD edit so the on-disk file always matches
    /// the live state.
    /// </summary>
    Task SaveAsync(string filePath, Tournament tournament, CancellationToken cancellationToken = default);
}
