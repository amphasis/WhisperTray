namespace WhisperTray.Core.Transcription;

public sealed record TranscriptionRequest
{
    public required byte[] AudioBytes { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
    public required string Model { get; init; }
    public string? Language { get; init; }
    public string? Prompt { get; init; }
}
