using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Publishing;

/// <summary>
/// Abstraction over "upload a tournament file to a publishing
/// destination". One implementation today
/// (<see cref="NaChessHubPublishingClient"/>); more can be added
/// behind the same interface (Lichess Team / Chess-Results / etc.)
/// without touching the Publishing dialog VM.
/// </summary>
public interface IPublishingClient
{
    /// <summary>Human-readable name shown in the destination picker.</summary>
    string DisplayName { get; }

    /// <summary>Uploads a tournament artefact.</summary>
    /// <param name="baseUrl">Root URL of the destination, e.g.
    /// <c>https://nachesshub.com</c>. Trailing slash optional.</param>
    /// <param name="eventId">Server-side id of the event to publish against.</param>
    /// <param name="passcode">Upload passcode / shared secret.</param>
    /// <param name="fileType">Kind of artefact being uploaded.</param>
    /// <param name="filePath">Absolute path on disk to the file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PublishResult> PublishAsync(
        string baseUrl,
        string eventId,
        string passcode,
        FileType fileType,
        string filePath,
        CancellationToken cancellationToken = default);
}
