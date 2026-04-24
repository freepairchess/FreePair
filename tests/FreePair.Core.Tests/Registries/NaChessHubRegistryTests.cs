using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.Registries;

namespace FreePair.Core.Tests.Registries;

/// <summary>
/// Tests for <see cref="NaChessHubRegistry"/> — exercises the two
/// HTTP endpoints through a stubbed <see cref="HttpMessageHandler"/>
/// so we can assert both request shape (URL + body) and response
/// parsing without hitting the network.
/// </summary>
public class NaChessHubRegistryTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public readonly List<HttpRequestMessage> Requests = new();
        public readonly List<string> RequestBodies = new();
        public Func<HttpRequestMessage, HttpResponseMessage>? Respond { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }
            else
            {
                RequestBodies.Add(string.Empty);
            }
            return Respond?.Invoke(request)
                   ?? new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DownloadSjson_posts_passcode_form_body_and_returns_payload()
    {
        var stub = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"Overview\":{}}")),
            }
        };
        using var http = new HttpClient(stub);
        var reg = new NaChessHubRegistry(http);

        var bytes = await reg.DownloadSjsonAsync(
            eventId: "0a9e8aa0-88e3-48fd-9fdf-2bff8850a7b9",
            passcode: "fb4177b5-c7e3-4095-8a49-dd83f3317825");

        Assert.Single(stub.Requests);
        Assert.Equal(HttpMethod.Post, stub.Requests[0].Method);
        Assert.Equal(
            "https://nachesshub.com/api/events/0a9e8aa0-88e3-48fd-9fdf-2bff8850a7b9/swisssysfile",
            stub.Requests[0].RequestUri!.ToString());
        Assert.Equal("passcode=fb4177b5-c7e3-4095-8a49-dd83f3317825", stub.RequestBodies[0]);
        Assert.StartsWith("{\"Overview", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task DownloadSjson_maps_401_to_RegistryException_with_passcode_hint()
    {
        var stub = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                ReasonPhrase = "Unauthorized",
            }
        };
        using var http = new HttpClient(stub);
        var reg = new NaChessHubRegistry(http);

        var ex = await Assert.ThrowsAsync<RegistryException>(() =>
            reg.DownloadSjsonAsync("e1", "bad"));
        Assert.Contains("passcode", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadSjson_maps_404_to_RegistryException_with_event_hint()
    {
        var stub = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = "Not Found" }
        };
        using var http = new HttpClient(stub);
        var reg = new NaChessHubRegistry(http);

        var ex = await Assert.ThrowsAsync<RegistryException>(() =>
            reg.DownloadSjsonAsync("missing-id", "pc"));
        Assert.Contains("missing-id", ex.Message);
    }

    [Fact]
    public async Task ListEvents_parses_expected_fields()
    {
        var json = """
        [
            {
                "id": "1a",
                "name": "Spring Open 2026",
                "startDate": "2026-04-01",
                "endDate":   "2026-04-02",
                "location":  "Seattle",
                "organizer": "NWChess"
            },
            {
                "id": "2b",
                "name": "Summer Rapid",
                "date": "2026-07-04"
            }
        ]
        """;
        var stub = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }
        };
        using var http = new HttpClient(stub);
        var reg = new NaChessHubRegistry(http);

        var events = await reg.ListEventsAsync();

        Assert.Equal(2, events.Count);
        var spring = events.Single(e => e.Id == "1a");
        Assert.Equal("Spring Open 2026", spring.Name);
        Assert.Equal(new DateOnly(2026, 4, 1), spring.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 2), spring.EndDate);
        Assert.Equal("Seattle", spring.Location);
        Assert.Equal("NWChess", spring.Organizer);

        var summer = events.Single(e => e.Id == "2b");
        // Field-alias fallback — `date` feeds StartDate.
        Assert.Equal(new DateOnly(2026, 7, 4), summer.StartDate);
    }

    [Fact]
    public async Task ListEvents_skips_entries_without_id_or_name()
    {
        var json = """[{ "id": "ok", "name": "Good" }, { "id": "" }, { "name": "only-name" }]""";
        var stub = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }
        };
        using var http = new HttpClient(stub);
        var reg = new NaChessHubRegistry(http);

        var events = await reg.ListEventsAsync();
        Assert.Single(events);
        Assert.Equal("ok", events[0].Id);
    }

    [Fact]
    public async Task ListEvents_uses_configured_base_url_and_path()
    {
        var stub = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]"),
            }
        };
        using var http = new HttpClient(stub);
        var reg = new NaChessHubRegistry(
            http,
            baseUrl: "https://staging.example.com/",
            listEventsPath: "/api/v2/tournaments");

        _ = await reg.ListEventsAsync();

        Assert.Equal(
            "https://staging.example.com/api/v2/tournaments",
            stub.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task ListEvents_parses_live_api_shape_with_datetime_and_split_address()
    {
        // Verbatim payload sample copied from the production
        // /api/events response: full datetime fields, split city /
        // state / zipCode, plus organizer + status. Earlier
        // implementations only knew about startDate / location
        // (single string), so the columns showed up empty for
        // every event.
        var json = """
        [
            {
                "id":"8435ae92-0c59-474e-bae9-3a79fffaeb40",
                "name":"8th Massachusetts Senior Open",
                "organizer":"Massachusetts Chess Assoc",
                "startDateTime":"2025-04-26T00:00:00",
                "endDateTime":"2025-04-27T00:00:00",
                "city":"Marlborough",
                "state":"MA",
                "zipCode":"01752",
                "status":"Completed"
            },
            {
                "id":"fa977038-1534-4035-9f6f-62f2cd477d75",
                "name":"MCC - 2025-04",
                "organizer":"Metrowest Chess Club",
                "startDateTime":"2025-04-02T19:30:00",
                "endDateTime":"2025-04-30T23:30:00",
                "city":"Framingham",
                "state":"MA",
                "zipCode":"01702",
                "status":"Completed"
            }
        ]
        """;
        var stub = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }
        };
        using var http = new HttpClient(stub);
        var reg = new NaChessHubRegistry(http);

        var events = await reg.ListEventsAsync();

        Assert.Equal(2, events.Count);
        var senior = events.Single(e => e.Id.StartsWith("8435"));
        Assert.Equal("8th Massachusetts Senior Open", senior.Name);
        Assert.Equal("Massachusetts Chess Assoc", senior.Organizer);
        Assert.Equal(new DateOnly(2025, 4, 26), senior.StartDate);
        Assert.Equal(new DateOnly(2025, 4, 27), senior.EndDate);
        Assert.Equal("Marlborough, MA 01752", senior.Location);
        Assert.Equal("Completed", senior.Status);
    }
}
