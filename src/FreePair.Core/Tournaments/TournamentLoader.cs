using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Default <see cref="ITournamentLoader"/> composed from the SwissSys
/// importer and domain mapper.
/// </summary>
public class TournamentLoader : ITournamentLoader
{
    private readonly SwissSysImporter _importer;

    public TournamentLoader() : this(new SwissSysImporter()) { }

    public TournamentLoader(SwissSysImporter importer)
    {
        _importer = importer ?? throw new System.ArgumentNullException(nameof(importer));
    }

    public async Task<Tournament> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var raw = await _importer.ImportAsync(filePath, cancellationToken).ConfigureAwait(false);
        return SwissSysMapper.Map(raw);
    }
}
