using System.Runtime.InteropServices;

namespace WhisperTray.Core.Audio;

/// <summary>Buffered capture output: 16-bit little-endian mono PCM at the given sample rate.</summary>
public sealed record RecordedAudio(byte[] PcmBytes, int SampleRate)
{
    public ReadOnlySpan<short> Samples => MemoryMarshal.Cast<byte, short>(PcmBytes);

    public TimeSpan Duration =>
        SampleRate > 0
            ? TimeSpan.FromSeconds((double)(PcmBytes.Length / sizeof(short)) / SampleRate)
            : TimeSpan.Zero;
}
