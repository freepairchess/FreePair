using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FreePair.Core.Settings;

namespace FreePair.Core.Registries;

/// <summary>
/// Composes the known <see cref="IExternalRegistry"/> providers
/// into a single list the UI can iterate over. Providers are
/// constructed from <see cref="AppSettings"/> so future base-URL /
/// endpoint overrides flow through without the UI having to know
/// which knobs belong to which provider.
/// </summary>
/// <remarks>
/// At v1 only NA Chess Hub is wired in. Adding Chess-Results etc.
/// later means dropping a new <see cref="IExternalRegistry"/>
/// implementation + one case in <see cref="Build"/> — no churn in
/// the dialogs or view-models.
/// </remarks>
public static class RegistryCatalog
{
    /// <summary>
    /// Builds the full list of registries available to the user.
    /// The <see cref="HttpClient"/> is shared across providers so
    /// connection pooling / HTTP/2 reuse kicks in; the caller owns
    /// its lifetime (typically one per app).
    /// </summary>
    public static IReadOnlyList<IExternalRegistry> Build(HttpClient http, AppSettings settings)
    {
        var list = new List<IExternalRegistry>();

        var naBase = string.IsNullOrWhiteSpace(settings.NaChessHubBaseUrl)
            ? NaChessHubRegistry.DefaultBaseUrl
            : settings.NaChessHubBaseUrl!.Trim();
        list.Add(new NaChessHubRegistry(http, naBase));

        return list;
    }

    /// <summary>
    /// Looks up a registry by its stable <see cref="IExternalRegistry.Key"/>.
    /// Returns <c>null</c> if the key is not present — UI code can
    /// fall back to the first entry.
    /// </summary>
    public static IExternalRegistry? Find(IEnumerable<IExternalRegistry> registries, string? key) =>
        registries.FirstOrDefault(r => string.Equals(r.Key, key, System.StringComparison.OrdinalIgnoreCase));
}
