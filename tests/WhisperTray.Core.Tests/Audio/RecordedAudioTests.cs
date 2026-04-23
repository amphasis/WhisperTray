using FluentAssertions;
using WhisperTray.Core.Audio;
using WhisperTray.Core.Tests.TestInfrastructure;

namespace WhisperTray.Core.Tests.Audio;

public class RecordedAudioTests
{
    [Fact]
    public void Samples_ReflectsBytesLength()
    {
        var pcm = AudioSamples.SamplesToPcmBytes(new short[] { 1, -1, 2, -2, 3, -3 });
        var audio = new RecordedAudio(pcm, 16_000);

        audio.Samples.Length.Should().Be(6);
    }

    [Fact]
    public void Duration_ForOneSecondOfSamples_IsOneSecond()
    {
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 1.0);
        var audio = new RecordedAudio(AudioSamples.SamplesToPcmBytes(samples), 16_000);

        audio.Duration.TotalSeconds.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void Duration_ForZeroSampleRate_IsZero()
    {
        var audio = new RecordedAudio(new byte[] { 1, 2, 3, 4 }, 0);

        audio.Duration.Should().Be(TimeSpan.Zero);
    }
}
