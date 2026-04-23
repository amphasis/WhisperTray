using WhisperTray.Core.Configuration;

namespace WhisperTray.Core.Transcription;

public static class ProviderModelCatalog
{
    private static readonly IReadOnlyDictionary<TranscriptionProvider, IReadOnlyList<string>> KnownModels =
        new Dictionary<TranscriptionProvider, IReadOnlyList<string>>
        {
            [TranscriptionProvider.OpenAi] = new[]
            {
                "whisper-1",
                "gpt-4o-transcribe",
                "gpt-4o-mini-transcribe",
            },
            [TranscriptionProvider.Lemonfox] = new[]
            {
                "whisper-1",
            },
            [TranscriptionProvider.HuggingFace] = new[]
            {
                "openai/whisper-large-v3",
                "openai/whisper-large-v3-turbo",
            },
        };

    public static IReadOnlyList<string> ModelsFor(TranscriptionProvider provider) =>
        KnownModels.TryGetValue(provider, out var models) ? models : Array.Empty<string>();

    public static bool IsKnown(TranscriptionProvider provider, string model) =>
        ModelsFor(provider).Contains(model, StringComparer.OrdinalIgnoreCase);
}
