using System.Text;
using Concentus;
using Concentus.Oggfile;
using FluentAssertions;
using WhisperTray.Core.Audio;
using WhisperTray.Core.Tests.TestInfrastructure;

namespace WhisperTray.Core.Tests.Audio;

public class OpusOggEncoderTests
{
    private readonly OpusOggEncoder _sut = new();

    [Fact]
    public void ContentType_IsAudioOgg()
    {
        _sut.ContentType.Should().Be("audio/ogg");
    }

    [Fact]
    public void FileExtension_IsDotOgg()
    {
        _sut.FileExtension.Should().Be(".ogg");
    }

    [Fact]
    public void Encode_OneSecondSine_ProducesNonEmptyBytes()
    {
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 1.0);

        var ogg = _sut.Encode(samples, 16_000);

        ogg.Should().NotBeEmpty();
    }

    [Fact]
    public void Encode_OutputBeginsWithOggsMagic()
    {
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 0.25);

        var ogg = _sut.Encode(samples, 16_000);

        Encoding.ASCII.GetString(ogg, 0, 4).Should().Be("OggS");
    }

    [Fact]
    public void Encode_CompressesHeavilyVersusWav()
    {
        // 1s of 16kHz 16-bit mono PCM is 32 KB. Opus at 16 kbps should fit in well
        // under a quarter of that. Generous threshold to stay stable across Concentus versions.
        var samples = AudioSamples.SynthesizeSine(16_000, 440, 1.0);
        var wavSize = new WavPassthroughEncoder().Encode(samples, 16_000).Length;

        var ogg = _sut.Encode(samples, 16_000);

        ogg.Length.Should().BeLessThan(wavSize / 3);
    }

    [Fact]
    public void Encode_RoundTripsThroughDecoder_YieldsNonTrivialSamples()
    {
        var sampleRate = 16_000;
        var samples = AudioSamples.SynthesizeSine(sampleRate, 440, 0.5);
        var ogg = _sut.Encode(samples, sampleRate);

        using var ms = new MemoryStream(ogg);
        var decoder = OpusCodecFactory.CreateDecoder(sampleRate, 1);
        var reader = new OpusOggReadStream(decoder, ms);

        var decoded = new List<short>();
        while (reader.HasNextPacket)
        {
            var packet = reader.DecodeNextPacket();
            if (packet is not null)
            {
                decoded.AddRange(packet);
            }
        }

        decoded.Should().NotBeEmpty("decoder should emit samples for a valid Opus stream");
        decoded.Any(s => s != 0).Should().BeTrue("the decoded signal shouldn't be pure silence");
    }

    [Fact]
    public void Encode_NegativeSampleRate_Throws()
    {
        var act = () => _sut.Encode(new short[] { 1, 2, 3 }, -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
