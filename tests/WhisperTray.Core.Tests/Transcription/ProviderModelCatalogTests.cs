using FluentAssertions;
using WhisperTray.Core.Configuration;
using WhisperTray.Core.Transcription;

namespace WhisperTray.Core.Tests.Transcription;

public class ProviderModelCatalogTests
{
    [Fact]
    public void OpenAi_IncludesGpt4oTranscribe()
    {
        ProviderModelCatalog.ModelsFor(TranscriptionProvider.OpenAi)
            .Should().Contain("gpt-4o-transcribe");
    }

    [Fact]
    public void OpenAi_IncludesWhisper1()
    {
        ProviderModelCatalog.ModelsFor(TranscriptionProvider.OpenAi)
            .Should().Contain("whisper-1");
    }

    [Fact]
    public void Lemonfox_OnlyKnowsWhisper1()
    {
        ProviderModelCatalog.ModelsFor(TranscriptionProvider.Lemonfox)
            .Should().BeEquivalentTo(new[] { "whisper-1" });
    }

    [Fact]
    public void Lemonfox_DoesNotKnowGpt4oTranscribe()
    {
        ProviderModelCatalog.IsKnown(TranscriptionProvider.Lemonfox, "gpt-4o-transcribe")
            .Should().BeFalse();
    }

    [Fact]
    public void HuggingFace_KnowsSomeWhisperVariant()
    {
        ProviderModelCatalog.ModelsFor(TranscriptionProvider.HuggingFace)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void IsKnown_IgnoresCasing()
    {
        ProviderModelCatalog.IsKnown(TranscriptionProvider.OpenAi, "WHISPER-1")
            .Should().BeTrue();
    }
}
