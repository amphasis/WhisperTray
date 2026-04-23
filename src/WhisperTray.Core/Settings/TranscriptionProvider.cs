namespace WhisperTray.Core.Configuration;

public enum TranscriptionProvider
{
    OpenAi,
    Lemonfox,
    HuggingFace,

    /// <summary>whisper-api.com — its own async-polling REST API, not OpenAI-compatible.</summary>
    WhisperApi,
}
