using System.Net;
using System.Text;
using FluentAssertions;
using WhisperTray.Core.Tests.TestInfrastructure;
using WhisperTray.Core.Transcription;

namespace WhisperTray.Core.Tests.Transcription;

public class WhisperApiClientTests
{
    private static readonly byte[] SampleAudio = Encoding.ASCII.GetBytes("FAKE-AUDIO-BYTES");

    private static TranscriptionRequest SampleRequest(string? language = null, string model = "base") =>
        new()
        {
            AudioBytes = SampleAudio,
            ContentType = "audio/ogg",
            FileName = "audio.ogg",
            Model = model,
            Language = language,
            Prompt = null,
        };

    private static (WhisperApiClient client, StubHttpMessageHandler handler) Build(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder,
        string baseUrl = "https://api.whisper-api.com")
    {
        var handler = new StubHttpMessageHandler { Responder = responder };
        var http = new HttpClient(handler);
        var client = new WhisperApiClient(
            http,
            baseUrl,
            "test-key",
            pollInterval: TimeSpan.Zero,
            pollBudget: TimeSpan.FromSeconds(10));
        return (client, handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task TranscribeAsync_HappyPath_ReturnsResultAfterPolling()
    {
        var step = 0;
        var (client, _) = Build((req, _) =>
        {
            step++;
            return step switch
            {
                1 => Task.FromResult(Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"queued"}""")),
                2 => Task.FromResult(Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"processing"}""")),
                _ => Task.FromResult(Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"completed","result":"hello","language":"en"}""")),
            };
        });

        var result = await client.TranscribeAsync(SampleRequest());

        result.Text.Should().Be("hello");
        result.DetectedLanguage.Should().Be("en");
    }

    [Fact]
    public async Task TranscribeAsync_SubmitPostsToTranscribeEndpoint()
    {
        var (client, handler) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"completed","result":"ok"}""")));

        await client.TranscribeAsync(SampleRequest());

        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].Uri.ToString().Should().Be("https://api.whisper-api.com/transcribe");
    }

    [Fact]
    public async Task TranscribeAsync_SendsXApiKeyHeader()
    {
        var (client, handler) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"completed","result":"ok"}""")));

        await client.TranscribeAsync(SampleRequest());

        handler.Requests[0].Headers["X-API-Key"].Should().Be("test-key");
    }

    [Fact]
    public async Task TranscribeAsync_MapsModelToModelSizeFormField()
    {
        var (client, handler) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"completed","result":"ok"}""")));

        await client.TranscribeAsync(SampleRequest(model: "large-v3"));

        var body = handler.Requests[0].BodyText!;
        body.Should().MatchRegex(@"name=""?model_size""?");
        body.Should().Contain("large-v3");
        body.Should().NotMatchRegex(@"name=""?model""?\b");
    }

    [Fact]
    public async Task TranscribeAsync_IncludesLanguageWhenSet()
    {
        var (client, handler) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"completed","result":"ok"}""")));

        await client.TranscribeAsync(SampleRequest(language: "ru"));

        handler.Requests[0].BodyText.Should().MatchRegex(@"name=""?language""?").And.Contain("ru");
    }

    [Fact]
    public async Task TranscribeAsync_OmitsLanguageWhenNull()
    {
        var (client, handler) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"completed","result":"ok"}""")));

        await client.TranscribeAsync(SampleRequest(language: null));

        handler.Requests[0].BodyText.Should().NotMatchRegex(@"name=""?language""?");
    }

    [Fact]
    public async Task TranscribeAsync_PollsStatusEndpointWithApiKey()
    {
        var step = 0;
        var (client, handler) = Build((req, _) =>
        {
            step++;
            return step == 1
                ? Task.FromResult(Json(HttpStatusCode.OK, """{"task_id":"T-99","status":"queued"}"""))
                : Task.FromResult(Json(HttpStatusCode.OK, """{"task_id":"T-99","status":"completed","result":"done"}"""));
        });

        await client.TranscribeAsync(SampleRequest());

        handler.Requests.Should().HaveCountGreaterOrEqualTo(2);
        handler.Requests[1].Method.Should().Be(HttpMethod.Get);
        handler.Requests[1].Uri.ToString().Should().Be("https://api.whisper-api.com/status/T-99");
        handler.Requests[1].Headers["X-API-Key"].Should().Be("test-key");
    }

    [Fact]
    public async Task TranscribeAsync_FailedStatus_ThrowsServerException()
    {
        var (client, _) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"failed","error":"corrupted file"}""")));

        var act = async () => await client.TranscribeAsync(SampleRequest());

        (await act.Should().ThrowAsync<TranscriptionServerException>())
            .Which.Message.Should().Contain("corrupted file");
    }

    [Fact]
    public async Task TranscribeAsync_Submit401_ThrowsAuthException()
    {
        var (client, _) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.Unauthorized, """{"error":"invalid api key"}""")));

        var act = async () => await client.TranscribeAsync(SampleRequest());

        await act.Should().ThrowAsync<TranscriptionAuthException>();
    }

    [Fact]
    public async Task TranscribeAsync_Submit429_ThrowsRateLimit()
    {
        var (client, _) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.TooManyRequests, """{"error":"slow down"}""")));

        var act = async () => await client.TranscribeAsync(SampleRequest());

        await act.Should().ThrowAsync<TranscriptionRateLimitException>();
    }

    [Fact]
    public async Task TranscribeAsync_PollingTimesOut_ThrowsServerException()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = (req, _) => Task.FromResult(
                Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"processing"}""")),
        };
        var http = new HttpClient(handler);
        var client = new WhisperApiClient(
            http,
            "https://api.whisper-api.com",
            "test-key",
            pollInterval: TimeSpan.Zero,
            pollBudget: TimeSpan.FromMilliseconds(50));

        var act = async () => await client.TranscribeAsync(SampleRequest());

        (await act.Should().ThrowAsync<TranscriptionServerException>())
            .Which.Message.Should().Contain("processing");
    }

    [Fact]
    public async Task TranscribeAsync_Cancelled_PropagatesCancellation()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
        };
        var client = new WhisperApiClient(new HttpClient(handler), "https://api.whisper-api.com", "k", pollInterval: TimeSpan.Zero);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await client.TranscribeAsync(SampleRequest(), cts.Token);

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task TranscribeAsync_MissingTaskId_ThrowsServerException()
    {
        var (client, _) = Build((req, _) => Task.FromResult(
            Json(HttpStatusCode.OK, """{"status":"queued"}""")));

        var act = async () => await client.TranscribeAsync(SampleRequest());

        (await act.Should().ThrowAsync<TranscriptionServerException>())
            .Which.Message.Should().Contain("task_id");
    }

    [Fact]
    public async Task TranscribeAsync_BaseUrlWithTrailingSlash_Works()
    {
        var (client, handler) = Build(
            (req, _) => Task.FromResult(
                Json(HttpStatusCode.OK, """{"task_id":"T-1","status":"completed","result":"ok"}""")),
            baseUrl: "https://api.whisper-api.com/");

        await client.TranscribeAsync(SampleRequest());

        handler.Requests[0].Uri.ToString().Should().Be("https://api.whisper-api.com/transcribe");
    }
}
