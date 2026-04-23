using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Publishing;

/// <summary>
/// <see cref="IPublishingClient"/> that uploads to the NAChessHub
/// <c>EventFilesAPI</c> controller (route
/// <c>POST /api/EventFilesAPI</c>). Matches the server signature
/// <c>PostEventFile(string eventId, string eventFileUploadPasscode,
/// FileType fileType, IFormFile file)</c>.
/// </summary>
/// <remarks>
/// The <see cref="HttpClient"/> is injected so callers can share a
/// long-lived instance (e.g. from <see cref="System.Net.Http.IHttpClientFactory"/>)
/// or plug in a stubbed <see cref="HttpMessageHandler"/> for tests.
/// </remarks>
public sealed class NaChessHubPublishingClient : IPublishingClient
{
    private readonly HttpClient _http;

    public NaChessHubPublishingClient(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    public string DisplayName => "NA Chess Hub";

    public async Task<PublishResult> PublishAsync(
        string baseUrl,
        string eventId,
        string passcode,
        FileType fileType,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(passcode);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return new PublishResult(false, ErrorMessage: $"File '{filePath}' does not exist.");
        }

        var root = baseUrl.TrimEnd('/');
        var url  = $"{root}/api/EventFilesAPI";

        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(eventId),          "eventId");
            form.Add(new StringContent(passcode),         "eventFileUploadPasscode");
            form.Add(new StringContent(((int)fileType).ToString()), "fileType");

            await using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            using var response = await _http.PostAsync(url, form, cancellationToken)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new PublishResult(
                    Success: false,
                    HttpStatusCode: (int)response.StatusCode,
                    ErrorMessage: $"{(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(body, 400)}");
            }

            return new PublishResult(
                Success: true,
                HttpStatusCode: (int)response.StatusCode,
                ServerFileId: TryExtractId(body));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            return new PublishResult(false,
                ErrorMessage: $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new PublishResult(false,
                ErrorMessage: $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Best-effort extraction of the server-assigned file id from a
    /// JSON response body. Returns null if the body isn't JSON or
    /// doesn't contain an <c>id</c> / <c>eventFileId</c> field.
    /// </summary>
    private static string? TryExtractId(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var name in new[] { "id", "eventFileId", "fileId" })
            {
                if (doc.RootElement.TryGetProperty(name, out var v))
                {
                    return v.ValueKind == JsonValueKind.Number
                        ? v.GetRawText()
                        : v.GetString();
                }
            }
        }
        catch (JsonException) { }
        return null;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
