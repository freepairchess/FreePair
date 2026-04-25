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
    private readonly string _eventWebUrlTemplate;

    public const string DefaultBaseUrl = "https://nachesshub.com";
    public const string DefaultListEventsPath = "/api/events";
    public const string DefaultDownloadSjsonPathTemplate = "/api/events/{eventId}/swisssysfile";

    /// <summary>
    /// Public website URL where TDs can read about an event in a
    /// browser. Note the <c>www.</c> prefix — this is intentionally
    /// a different host from the API root (<see cref="DefaultBaseUrl"/>).
    /// </summary>
    public const string DefaultEventWebUrlTemplate = "https://www.nachesshub.com/Events/Details/{eventId}";

    public string Key => "nachesshub";
    public string DisplayName => "NA Chess Hub";
    public bool SupportsListEvents => true;

    public NaChessHubRegistry(
        HttpClient http,
        string baseUrl = DefaultBaseUrl,
        string listEventsPath = DefaultListEventsPath,
        string downloadSjsonPathTemplate = DefaultDownloadSjsonPathTemplate,
        string eventWebUrlTemplate = DefaultEventWebUrlTemplate)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventWebUrlTemplate);
        _baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        _listEventsPath = listEventsPath.TrimStart('/');
        _downloadSjsonPathTemplate = downloadSjsonPathTemplate.TrimStart('/');
        _eventWebUrlTemplate = eventWebUrlTemplate;
    }

    public string? GetEventWebUrl(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return null;
        return _eventWebUrlTemplate.Replace("{eventId}", Uri.EscapeDataString(eventId.Trim()));
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

            // Date fields: prefer the full datetime fields the live
            // API uses (startDateTime / endDateTime in YYYY-MM-DDTHH:MM:SS
            // form); fall back to the date-only / generic-"date"
            // aliases that earlier API revisions produced.
            var start = TryParseDate(e.StartDateTime ?? e.StartDate ?? e.Start ?? e.Date);
            var end   = TryParseDate(e.EndDateTime   ?? e.EndDate   ?? e.End);

            // Location: the live API splits address into city / state
            // / zipCode. Compose them into a single display string;
            // fall back to a flat 'location' field if a future revision
            // collapses them again.
            var location = ComposeLocation(e.City, e.State, e.ZipCode)
                           ?? (string.IsNullOrWhiteSpace(e.Location) ? null : e.Location);

            list.Add(new RegistryEvent(
                Id: e.Id!,
                Name: e.Name!,
                StartDate: start,
                EndDate:   end,
                Location:  location,
                Organizer: string.IsNullOrWhiteSpace(e.Organizer) ? null : e.Organizer,
                Status:    string.IsNullOrWhiteSpace(e.Status) ? null : e.Status));
        }
        return list;
    }

    /// <summary>
    /// Builds a "City, ST" / "City, ST 01752" display string from
    /// the per-field components. Returns <c>null</c> when no
    /// component was supplied so the fallback to a flat
    /// <c>location</c> field kicks in.
    /// </summary>
    private static string? ComposeLocation(string? city, string? state, string? zipCode)
    {
        var parts = new List<string>(2);
        var cityState = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(city))  cityState.Add(city.Trim());
        if (!string.IsNullOrWhiteSpace(state)) cityState.Add(state.Trim());
        if (cityState.Count > 0) parts.Add(string.Join(", ", cityState));
        if (!string.IsNullOrWhiteSpace(zipCode)) parts.Add(zipCode.Trim());
        return parts.Count == 0 ? null : string.Join(" ", parts);
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
        // Live API format is "2025-04-26T00:00:00" — DateTime parses
        // both that and bare "2025-04-26"; we strip down to DateOnly
        // because the dialog grid only shows the calendar date.
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }
        return DateOnly.TryParse(s, out var d) ? d : null;
    }

    private sealed class RawEvent
    {
        [JsonPropertyName("id")]            public string? Id { get; set; }
        [JsonPropertyName("name")]          public string? Name { get; set; }
        [JsonPropertyName("organizer")]     public string? Organizer { get; set; }
        [JsonPropertyName("status")]        public string? Status { get; set; }

        // Live API uses these full-datetime forms.
        [JsonPropertyName("startDateTime")] public string? StartDateTime { get; set; }
        [JsonPropertyName("endDateTime")]   public string? EndDateTime { get; set; }

        // Earlier-revision aliases kept for forward-compat / staging
        // environments that may still emit the date-only form.
        [JsonPropertyName("startDate")]     public string? StartDate { get; set; }
        [JsonPropertyName("endDate")]       public string? EndDate { get; set; }
        [JsonPropertyName("start")]         public string? Start { get; set; }
        [JsonPropertyName("end")]           public string? End { get; set; }
        [JsonPropertyName("date")]          public string? Date { get; set; }

        // Live API splits address; the legacy flat 'location' is the
        // last-resort fallback.
        [JsonPropertyName("city")]          public string? City { get; set; }
        [JsonPropertyName("state")]         public string? State { get; set; }
        [JsonPropertyName("zipCode")]       public string? ZipCode { get; set; }
        [JsonPropertyName("location")]      public string? Location { get; set; }
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
