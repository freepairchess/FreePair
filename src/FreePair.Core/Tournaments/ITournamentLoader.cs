using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Loads a tournament from a SwissSys <c>.sjson</c> file into the FreePair
/// domain model.
/// </summary>
public interface ITournamentLoader
{
    /// <summary>
    /// Parses the file at <paramref name="filePath"/> and maps it into a
    /// <see cref="Tournament"/>. Throws when the file is missing or invalid.
    /// </summary>
    Task<Tournament> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
