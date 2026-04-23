using System.Text;
using FluentAssertions;
using WhisperTray.Core.Audio;
using WhisperTray.Core.Tests.TestInfrastructure;

namespace WhisperTray.Core.Tests.Audio;

public class WavPassthroughEncoderTests
{
    private readonly WavPassthroughEncoder _sut = new();

    [Fact]
    public void ContentType_IsAudioWav()
    {
        _sut.ContentType.Should().Be("audio/wav");
    }

    [Fact]
    public void FileExtension_IsDotWav()
    {
        _sut.FileExtension.Should().Be(".wav");
    }

    [Fact]
    public void Encode_StartsWithRiffWaveMagic()
    {
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 0.1);

        var wav = _sut.Encode(samples, 16_000);

        Encoding.ASCII.GetString(wav, 0, 4).Should().Be("RIFF");
        Encoding.ASCII.GetString(wav, 8, 4).Should().Be("WAVE");
        Encoding.ASCII.GetString(wav, 12, 4).Should().Be("fmt ");
        Encoding.ASCII.GetString(wav, 36, 4).Should().Be("data");
    }

    [Fact]
    public void Encode_EmbedsSampleRateInFmtChunk()
    {
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 0.1);

        var wav = _sut.Encode(samples, 16_000);

        var sampleRate = BitConverter.ToInt32(wav, 24);
        sampleRate.Should().Be(16_000);
    }

    [Fact]
    public void Encode_EmbedsCorrectDataChunkSize()
    {
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 0.1);

        var wav = _sut.Encode(samples, 16_000);

        var dataSize = BitConverter.ToInt32(wav, 40);
        dataSize.Should().Be(samples.Length * sizeof(short));
    }

    [Fact]
    public void Encode_PreservesSamplesExactly()
    {
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 0.1);

        var wav = _sut.Encode(samples, 16_000);

        var roundTripped = new short[samples.Length];
        Buffer.BlockCopy(wav, 44, roundTripped, 0, samples.Length * sizeof(short));
        roundTripped.Should().BeEquivalentTo(samples);
    }

    [Fact]
    public void Encode_EmptyInput_StillProducesValidHeader()
    {
        var wav = _sut.Encode(Array.Empty<short>(), 16_000);

        wav.Length.Should().Be(44);
        Encoding.ASCII.GetString(wav, 0, 4).Should().Be("RIFF");
    }

    [Fact]
    public void Encode_NegativeSampleRate_Throws()
    {
        var act = () => _sut.Encode(new short[] { 1, 2, 3 }, -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Encode_ZeroSampleRate_Throws()
    {
        var act = () => _sut.Encode(new short[] { 1, 2, 3 }, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
