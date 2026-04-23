using System.Runtime.InteropServices;
using System.Text;

namespace WhisperTray.Core.Audio;

/// <summary>Writes raw PCM as a minimal RIFF/WAVE container. No compression.</summary>
public sealed class WavPassthroughEncoder : IAudioEncoder
{
    public string ContentType => "audio/wav";
    public string FileExtension => ".wav";

    public byte[] Encode(ReadOnlySpan<short> pcm, int sampleRate)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Sample rate must be positive.");
        }

        const short Channels = 1;
        const short BitsPerSample = 16;
        const short BlockAlign = Channels * BitsPerSample / 8;
        var byteRate = sampleRate * BlockAlign;
        var dataSize = pcm.Length * sizeof(short);

        using var ms = new MemoryStream(44 + dataSize);
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);

        bw.Write("fmt "u8);
        bw.Write(16);                    // PCM fmt chunk size
        bw.Write((short)1);              // PCM format
        bw.Write(Channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(BlockAlign);
        bw.Write(BitsPerSample);

        bw.Write("data"u8);
        bw.Write(dataSize);
        bw.Write(MemoryMarshal.AsBytes(pcm));

        bw.Flush();
        return ms.ToArray();
    }
}
