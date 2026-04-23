namespace FreePair.Core.Publishing;

/// <summary>
/// Outcome of a single publish attempt against an
/// <see cref="IPublishingClient"/>.
/// </summary>
/// <param name="Success">True when the server returned a 2xx response.</param>
/// <param name="HttpStatusCode">Raw HTTP status code returned by
/// the server, or <see langword="null"/> if the request never reached
/// the server (network failure, DNS error, etc.).</param>
/// <param name="ErrorMessage">Human-readable summary of the failure
/// suitable for display in the error banner; <see langword="null"/>
/// on success.</param>
/// <param name="ServerFileId">Opaque id assigned by the server to
/// the uploaded artefact when available (e.g. to let the user jump
/// to it in a browser). <see langword="null"/> if the server didn't
/// return one or on failure.</param>
public sealed record PublishResult(
    bool Success,
    int? HttpStatusCode = null,
    string? ErrorMessage = null,
    string? ServerFileId = null);
