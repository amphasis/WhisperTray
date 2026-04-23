using System.Net;

namespace WhisperTray.Core.Transcription;

public class TranscriptionException : Exception
{
    public TranscriptionException(string message) : base(message) { }
    public TranscriptionException(string message, Exception inner) : base(message, inner) { }
}

public sealed class TranscriptionAuthException : TranscriptionException
{
    public TranscriptionAuthException(string message) : base(message) { }
}

public sealed class TranscriptionRateLimitException : TranscriptionException
{
    public TranscriptionRateLimitException(string message) : base(message) { }
}

public sealed class TranscriptionRequestException : TranscriptionException
{
    public HttpStatusCode StatusCode { get; }

    public TranscriptionRequestException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class TranscriptionServerException : TranscriptionException
{
    public HttpStatusCode? StatusCode { get; }

    public TranscriptionServerException(HttpStatusCode? statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public TranscriptionServerException(string message, Exception inner) : base(message, inner)
    {
        StatusCode = null;
    }
}
