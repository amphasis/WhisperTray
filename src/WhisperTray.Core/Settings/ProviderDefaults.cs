namespace WhisperTray.Core.Configuration;

/// <summary>
/// Default BaseUrl / Model per provider — used by the settings window to prefill
/// sensible values when the user switches providers. The values line up with
/// what each vendor documents as the production endpoint.
/// </summary>
public static class ProviderDefaults
{
    public static string DefaultBaseUrl(TranscriptionProvider provider) => provider switch
    {
        TranscriptionProvider.OpenAi => "https://api.openai.com/v1",
        TranscriptionProvider.Lemonfox => "https://api.lemonfox.ai/v1",
        TranscriptionProvider.HuggingFace => "https://api-inference.huggingface.co",
        TranscriptionProvider.WhisperApi => "https://api.whisper-api.com",
        _ => "https://api.openai.com/v1",
    };

    public static string DefaultModel(TranscriptionProvider provider) => provider switch
    {
        TranscriptionProvider.OpenAi => "gpt-4o-transcribe",
        TranscriptionProvider.Lemonfox => "whisper-1",
        TranscriptionProvider.HuggingFace => "openai/whisper-large-v3",
        TranscriptionProvider.WhisperApi => "base",
        _ => "gpt-4o-transcribe",
    };
}
