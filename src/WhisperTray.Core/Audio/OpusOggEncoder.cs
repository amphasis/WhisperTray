using Concentus;
using Concentus.Enums;
using Concentus.Oggfile;

namespace WhisperTray.Core.Audio;

/// <summary>Compresses PCM with Opus and wraps the result in an OGG container.</summary>
public sealed class OpusOggEncoder : IAudioEncoder
{
    private const int TargetBitrateBitsPerSecond = 16_000;

    public string ContentType => "audio/ogg";
    public string FileExtension => ".ogg";

    public byte[] Encode(ReadOnlySpan<short> pcm, int sampleRate)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Sample rate must be positive.");
        }

        var encoder = OpusCodecFactory.CreateEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = TargetBitrateBitsPerSecond;

        using var ms = new MemoryStream();
        var oggStream = new OpusOggWriteStream(encoder, ms, inputSampleRate: sampleRate);

        // OpusOggWriteStream requires a short[] buffer; copy the span once.
        var buffer = pcm.ToArray();
        oggStream.WriteSamples(buffer, 0, buffer.Length);
        oggStream.Finish();

        return ms.ToArray();
    }
}
