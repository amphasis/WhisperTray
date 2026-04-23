using System.Net;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = new();

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Responder { get; set; }

    public static StubHttpMessageHandler RespondingWith(HttpStatusCode status, string body, string contentType = "application/json")
    {
        return new StubHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, contentType),
            }),
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? bodyText = null;
        byte[]? bodyBytes = null;
        if (request.Content is not null)
        {
            bodyBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            // Multipart bodies are binary — try UTF-8 decode for inspection; callers only use this for string-safe asserts.
            bodyText = System.Text.Encoding.UTF8.GetString(bodyBytes);
        }

        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase),
            request.Content?.Headers.ContentType?.MediaType,
            bodyBytes,
            bodyText));

        if (Responder is null)
        {
            return new HttpResponseMessage(HttpStatusCode.NotImplemented);
        }

        return await Responder(request, cancellationToken).ConfigureAwait(false);
    }

    public sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        IReadOnlyDictionary<string, string> Headers,
        string? ContentTypeMediaType,
        byte[]? BodyBytes,
        string? BodyText);
}
