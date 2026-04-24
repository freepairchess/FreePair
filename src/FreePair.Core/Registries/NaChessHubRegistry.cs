using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Registries;

/// <summary>
/// <see cref="IExternalRegistry"/> implementation for
/// <a href="https://nachesshub.com">NA Chess Hub</a>. Two endpoints:
/// <list type="bullet">
///   <item><c>GET  {baseUrl}/api/events</c> → JSON array of events.</item>
///   <item><c>POST {baseUrl}/api/events/{eventId}/swisssysfile</c>
///         with form-encoded <c>passcode={...}</c> → the <c>.sjson</c>
///         payload (bytes).</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>The concrete URL paths are stored on the instance so a
/// future Settings UI can override them (useful for staging / local
/// dev endpoints). The <see cref="HttpClient"/> is supplied by the
/// caller; the Core library doesn't own HttpClient lifecycle.</para>
/// </remarks>
public sealed class NaChessHubRegistry : IExternalRegistry
{
    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly string _listEventsPath;
    private readonly string _downloadSjsonPathTemplate;

    public const string DefaultBaseUrl = "https://nachesshub.com";
    public const string DefaultListEventsPath = "/api/events";
    public const string DefaultDownloadSjsonPathTemplate = "/api/events/{eventId}/swisssysfile";

    public string Key => "nachesshub";
    public string DisplayName => "NA Chess Hub";
    public bool SupportsListEvents => true;

    public NaChessHubRegistry(
        HttpClient http,
        string baseUrl = DefaultBaseUrl,
        string listEventsPath = DefaultListEventsPath,
        string downloadSjsonPathTemplate = DefaultDownloadSjsonPathTemplate)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        _baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        _listEventsPath = listEventsPath.TrimStart('/');
        _downloadSjsonPathTemplate = downloadSjsonPathTemplate.TrimStart('/');
    }

    public async Task<byte[]> DownloadSjsonAsync(
        string eventId,
        string passcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(passcode);

        var path = _downloadSjsonPathTemplate.Replace("{eventId}", Uri.EscapeDataString(eventId.Trim()));
        var url = new Uri(_baseUri, path);

        // NA Chess Hub's sample call uses application/x-www-form-urlencoded
        // with the passcode as a single body field — matches the
        // PowerShell Invoke-WebRequest -Body @{ passcode = "..." } idiom.
        using var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("passcode", passcode.Trim()),
        });

        using var response = await _http.PostAsync(url, body, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw MapHttpError(response, "download .sjson", eventId);
        }
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RegistryEvent>> ListEventsAsync(
        CancellationToken cancellationToken = default)
    {
        var url = new Uri(_baseUri, _listEventsPath);
        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw MapHttpError(response, "list events", null);
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        // NA Chess Hub's JSON shape is best-effort — field names
        // drift across releases. We pull a small, stable subset and
        // let unknown keys fall through.
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };
        var raw = await JsonSerializer.DeserializeAsync<List<RawEvent>>(stream, options, cancellationToken)
                    .ConfigureAwait(false)
                  ?? new List<RawEvent>();

        var list = new List<RegistryEvent>(raw.Count);
        foreach (var e in raw)
        {
            if (string.IsNullOrWhiteSpace(e.Id) || string.IsNullOrWhiteSpace(e.Name)) continue;
            list.Add(new RegistryEvent(
                Id: e.Id!,
                Name: e.Name!,
                StartDate: TryParseDate(e.StartDate ?? e.Start ?? e.Date),
                EndDate:   TryParseDate(e.EndDate ?? e.End),
                Location:  string.IsNullOrWhiteSpace(e.Location) ? null : e.Location,
                Organizer: string.IsNullOrWhiteSpace(e.Organizer) ? null : e.Organizer));
        }
        return list;
    }

    private static Exception MapHttpError(HttpResponseMessage response, string op, string? eventId)
    {
        var hint = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden  => " (check the passcode)",
            HttpStatusCode.NotFound          => eventId is null ? "" : $" (event '{eventId}' not found)",
            _                                => string.Empty,
        };
        return new RegistryException(
            $"NA Chess Hub {op} failed: {(int)response.StatusCode} {response.ReasonPhrase}{hint}.");
    }

    private static DateOnly? TryParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateOnly.TryParse(s, out var d) ? d : null;
    }

    private sealed class RawEvent
    {
        [JsonPropertyName("id")]        public string? Id { get; set; }
        [JsonPropertyName("name")]      public string? Name { get; set; }
        [JsonPropertyName("startDate")] public string? StartDate { get; set; }
        [JsonPropertyName("endDate")]   public string? EndDate { get; set; }
        [JsonPropertyName("start")]     public string? Start { get; set; }
        [JsonPropertyName("end")]       public string? End { get; set; }
        [JsonPropertyName("date")]      public string? Date { get; set; }
        [JsonPropertyName("location")]  public string? Location { get; set; }
        [JsonPropertyName("organizer")] public string? Organizer { get; set; }
    }
}

/// <summary>
/// Thrown by <see cref="IExternalRegistry"/> implementations when an
/// HTTP call fails with a status code the caller is unlikely to
/// recover from without user action (wrong credentials, bad event
/// ID, offline). The exception message is user-facing.
/// </summary>
public sealed class RegistryException : Exception
{
    public RegistryException(string message) : base(message) { }
    public RegistryException(string message, Exception inner) : base(message, inner) { }
}
