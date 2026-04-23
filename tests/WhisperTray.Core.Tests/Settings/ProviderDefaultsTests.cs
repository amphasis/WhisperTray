using FluentAssertions;
using WhisperTray.Core.Configuration;

namespace WhisperTray.Core.Tests;

public class ProviderDefaultsTests
{
    [Theory]
    [InlineData(TranscriptionProvider.OpenAi, "https://api.openai.com/v1")]
    [InlineData(TranscriptionProvider.Lemonfox, "https://api.lemonfox.ai/v1")]
    [InlineData(TranscriptionProvider.HuggingFace, "https://api-inference.huggingface.co")]
    [InlineData(TranscriptionProvider.WhisperApi, "https://api.whisper-api.com")]
    public void DefaultBaseUrl_ReturnsVendorEndpoint(TranscriptionProvider provider, string expected)
    {
        ProviderDefaults.DefaultBaseUrl(provider).Should().Be(expected);
    }

    [Theory]
    [InlineData(TranscriptionProvider.OpenAi, "gpt-4o-transcribe")]
    [InlineData(TranscriptionProvider.Lemonfox, "whisper-1")]
    [InlineData(TranscriptionProvider.HuggingFace, "openai/whisper-large-v3")]
    [InlineData(TranscriptionProvider.WhisperApi, "base")]
    public void DefaultModel_ReturnsAKnownModel(TranscriptionProvider provider, string expected)
    {
        ProviderDefaults.DefaultModel(provider).Should().Be(expected);
        // Sanity: the default we advertise must also be in the catalog.
        WhisperTray.Core.Transcription.ProviderModelCatalog.IsKnown(provider, expected).Should().BeTrue();
    }
}
