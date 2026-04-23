using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhisperTray.Core.Transcription;

/// <summary>
/// Client for whisper-api.com (not to be confused with OpenAI or Lemonfox).
/// Protocol:
///   - POST {baseUrl}/transcribe  with X-API-Key header and multipart body
///     ("file", plus optional "model_size" / "language" / "format").
///     Response is immediate: { task_id, status: "queued" | "processing", ... }.
///   - GET  {baseUrl}/status/{task_id}  with the same header, polled until
///     status is "completed" (result in "result") or "failed".
/// </summary>
public sealed class WhisperApiClient : ITranscriptionClient
{
    private static readonly JsonSerializerOptions ResponseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _http;
    private readonly Uri _submitEndpoint;
    private readonly string _statusBase;
    private readonly string _apiKey;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _pollBudget;

    public WhisperApiClient(
        HttpClient http,
        string baseUrl,
        string apiKey,
        TimeSpan? pollInterval = null,
        TimeSpan? pollBudget = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        _http = http;
        var trimmed = baseUrl.TrimEnd('/');
        _submitEndpoint = new Uri($"{trimmed}/transcribe", UriKind.Absolute);
        _statusBase = $"{trimmed}/status/";
        _apiKey = apiKey;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(750);
        _pollBudget = pollBudget ?? TimeSpan.FromMinutes(5);
    }

    public async Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var taskId = await SubmitAsync(request, cancellationToken).ConfigureAwait(false);
        return await PollAsync(taskId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> SubmitAsync(TranscriptionRequest request, CancellationToken cancellationToken)
    {
        using var form = BuildForm(request);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _submitEndpoint) { Content = form };
        httpRequest.Headers.Add("X-API-Key", _apiKey);

        using var response = await SendOrThrow(httpRequest, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BuildException(response.StatusCode, body);
        }

        var payload = DeserializeStatusOrThrow(body);
        if (string.IsNullOrEmpty(payload.TaskId))
        {
            throw new TranscriptionServerException(null, "whisper-api.com submit response contained no task_id.");
        }
        return payload.TaskId;
    }

    private async Task<TranscriptionResult> PollAsync(string taskId, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + _pollBudget;
        var statusUri = new Uri(_statusBase + Uri.EscapeDataString(taskId), UriKind.Absolute);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, statusUri);
            httpRequest.Headers.Add("X-API-Key", _apiKey);

            using var response = await SendOrThrow(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildException(response.StatusCode, body);
            }

            var payload = DeserializeStatusOrThrow(body);
            switch (payload.Status)
            {
                case "completed":
                    if (string.IsNullOrEmpty(payload.Result))
                    {
                        throw new TranscriptionServerException(null, "whisper-api.com returned completed status but no result.");
                    }
                    return new TranscriptionResult(payload.Result, payload.Language);

                case "failed":
                    throw new TranscriptionServerException(null,
                        $"whisper-api.com reported a failed transcription: {payload.Error ?? "(no details)"}");

                case "queued":
                case "processing":
                case "pending":
                    if (DateTimeOffset.UtcNow > deadline)
                    {
                        throw new TranscriptionServerException(null,
                            $"whisper-api.com still reports status={payload.Status} after {_pollBudget}.");
                    }
                    await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new TranscriptionServerException(null,
                        $"whisper-api.com returned unexpected status: {payload.Status}");
            }
        }
    }

    private async Task<HttpResponseMessage> SendOrThrow(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new TranscriptionServerException("Transport error while contacting whisper-api.com.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TranscriptionServerException("whisper-api.com request timed out.", ex);
        }
    }

    private static MultipartFormDataContent BuildForm(TranscriptionRequest request)
    {
        var form = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(request.AudioBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
        form.Add(fileContent, "file", request.FileName);

        // TranscriptionRequest.Model maps to whisper-api.com's "model_size" form field.
        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            form.Add(new StringContent(request.Model), "model_size");
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            form.Add(new StringContent(request.Language), "language");
        }

        // Prompt hinting isn't part of whisper-api.com's contract; silently dropped.
        return form;
    }

    private static StatusPayload DeserializeStatusOrThrow(string body)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<StatusPayload>(body, ResponseOptions);
            if (payload is null)
            {
                throw new TranscriptionServerException(null, "whisper-api.com returned an empty JSON body.");
            }
            return payload;
        }
        catch (JsonException ex)
        {
            throw new TranscriptionServerException("Could not parse whisper-api.com response: " + ex.Message, ex);
        }
    }

    private static TranscriptionException BuildException(HttpStatusCode status, string body)
    {
        var message = ExtractErrorMessage(body) ?? $"HTTP {(int)status} {status}";
        return status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new TranscriptionAuthException(message),
            HttpStatusCode.TooManyRequests =>
                new TranscriptionRateLimitException(message),
            >= HttpStatusCode.InternalServerError =>
                new TranscriptionServerException(status, message),
            _ =>
                new TranscriptionRequestException(status, message),
        };
    }

    private static string? ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.ValueKind == JsonValueKind.String)
                {
                    return errorElement.GetString();
                }
                if (errorElement.ValueKind == JsonValueKind.Object
                    && errorElement.TryGetProperty("message", out var msg)
                    && msg.ValueKind == JsonValueKind.String)
                {
                    return msg.GetString();
                }
            }
            if (doc.RootElement.TryGetProperty("message", out var topMsg) && topMsg.ValueKind == JsonValueKind.String)
            {
                return topMsg.GetString();
            }
        }
        catch (JsonException)
        {
            // fall through
        }
        return body.Length > 256 ? body[..256] : body;
    }

    private sealed class StatusPayload
    {
        [JsonPropertyName("task_id")]
        public string? TaskId { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("result")]
        public string? Result { get; init; }

        [JsonPropertyName("language")]
        public string? Language { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}
