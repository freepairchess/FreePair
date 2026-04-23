using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.Publishing;

namespace FreePair.Core.Tests.Publishing;

public class NaChessHubPublishingClientTests
{
    private static string WriteTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"fp-publish-{Guid.NewGuid():N}.sjson");
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Minimal <see cref="HttpMessageHandler"/> that records the
    /// last request and returns a canned response.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":42}"),
        };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content
                    .ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            return Response;
        }
    }

    [Fact]
    public async Task PublishAsync_posts_to_api_EventFilesAPI_with_multipart_fields()
    {
        var handler = new CapturingHandler();
        var client  = new NaChessHubPublishingClient(new HttpClient(handler));

        var tmp = WriteTempFile("{\"Overview\":{}}");
        try
        {
            var result = await client.PublishAsync(
                baseUrl:  "https://nachesshub.com",
                eventId:  "evt-123",
                passcode: "secret-pass",
                fileType: FileType.SwissSys11SJson,
                filePath: tmp);

            Assert.True(result.Success);
            Assert.Equal(200, result.HttpStatusCode);
            Assert.Equal("42", result.ServerFileId);

            Assert.NotNull(handler.LastRequest);
            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("https://nachesshub.com/api/EventFilesAPI",
                handler.LastRequest.RequestUri!.ToString());

            // Multipart body must contain all four fields. We don't
            // assert the exact Content-Disposition header formatting
            // (name="foo" vs. name=foo) because that's an .NET impl
            // detail — instead we check that every field name AND
            // every field value is present in the body somewhere.
            Assert.NotNull(handler.LastRequestBody);
            var body = handler.LastRequestBody!;
            Assert.Contains("eventId",                 body);
            Assert.Contains("evt-123",                 body);
            Assert.Contains("eventFileUploadPasscode", body);
            Assert.Contains("secret-pass",             body);
            Assert.Contains("fileType",                body);
            // SwissSys11SJson = 10 — sent as an int.
            Assert.Contains("10",                      body);
            // The file part's filename is the temp filename we wrote.
            Assert.Contains(Path.GetFileName(tmp),     body);
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public async Task PublishAsync_trims_trailing_slash_on_base_url()
    {
        var handler = new CapturingHandler();
        var client  = new NaChessHubPublishingClient(new HttpClient(handler));

        var tmp = WriteTempFile("x");
        try
        {
            await client.PublishAsync("https://nachesshub.com/", "e", "p", FileType.Pairing, tmp);
            Assert.Equal("https://nachesshub.com/api/EventFilesAPI",
                handler.LastRequest!.RequestUri!.ToString());
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public async Task PublishAsync_maps_non_2xx_response_to_failure_with_status_and_body()
    {
        var handler = new CapturingHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Invalid passcode"),
            },
        };
        var client = new NaChessHubPublishingClient(new HttpClient(handler));

        var tmp = WriteTempFile("x");
        try
        {
            var result = await client.PublishAsync(
                "https://nachesshub.com", "e", "wrong-pass",
                FileType.SwissSys11SJson, tmp);

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpStatusCode);
            Assert.Contains("401",             result.ErrorMessage);
            Assert.Contains("Invalid passcode", result.ErrorMessage);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public async Task PublishAsync_catches_network_errors_into_PublishResult()
    {
        // Handler that always throws — simulates DNS / socket failure.
        var handler = new ThrowingHandler(new HttpRequestException("DNS failure"));
        var client  = new NaChessHubPublishingClient(new HttpClient(handler));

        var tmp = WriteTempFile("x");
        try
        {
            var result = await client.PublishAsync(
                "https://nachesshub.com", "e", "p",
                FileType.SwissSys11SJson, tmp);

            Assert.False(result.Success);
            Assert.Null(result.HttpStatusCode);
            Assert.Contains("Network error", result.ErrorMessage);
            Assert.Contains("DNS failure",   result.ErrorMessage);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public async Task PublishAsync_returns_failure_when_file_does_not_exist()
    {
        var handler = new CapturingHandler();
        var client  = new NaChessHubPublishingClient(new HttpClient(handler));

        var result = await client.PublishAsync(
            "https://nachesshub.com", "e", "p",
            FileType.SwissSys11SJson,
            Path.Combine(Path.GetTempPath(), "does-not-exist.sjson"));

        Assert.False(result.Success);
        Assert.Null(handler.LastRequest); // no HTTP call attempted
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task PublishAsync_propagates_OperationCanceledException()
    {
        var handler = new ThrowingHandler(new OperationCanceledException());
        var client  = new NaChessHubPublishingClient(new HttpClient(handler));

        var tmp = WriteTempFile("x");
        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                client.PublishAsync("https://nachesshub.com", "e", "p",
                    FileType.SwissSys11SJson, tmp));
        }
        finally { File.Delete(tmp); }
    }

    private sealed class ThrowingHandler(Exception ex) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw ex;
    }
}
