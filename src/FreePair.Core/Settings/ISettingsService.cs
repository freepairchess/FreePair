using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Settings;

public interface ISettingsService
{
    /// <summary>
    /// Loads settings from persistent storage. Returns a new empty <see cref="AppSettings"/>
    /// instance if no settings have been saved yet.
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the supplied settings to storage.
    /// </summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
