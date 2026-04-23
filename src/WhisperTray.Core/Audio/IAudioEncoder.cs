namespace WhisperTray.Core.Audio;

public interface IAudioEncoder
{
    /// <summary>MIME type of the produced bytes (e.g. "audio/ogg", "audio/wav").</summary>
    string ContentType { get; }

    /// <summary>File extension including the dot (e.g. ".ogg", ".wav").</summary>
    string FileExtension { get; }

    /// <summary>
    /// Encodes 16-bit little-endian mono PCM samples at the given sample rate
    /// into a self-contained container blob.
    /// </summary>
    byte[] Encode(ReadOnlySpan<short> pcm, int sampleRate);
}
