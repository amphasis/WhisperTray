using WhisperTray.Core.Audio;
using AudioFormatEnum = WhisperTray.Core.Configuration.AudioFormat;

namespace WhisperTray.Core.Orchestration;

public interface IAudioEncoderFactory
{
    IAudioEncoder Create(AudioFormatEnum format);
}

public sealed class DefaultAudioEncoderFactory : IAudioEncoderFactory
{
    public IAudioEncoder Create(AudioFormatEnum format) => format switch
    {
        AudioFormatEnum.OggOpus => new OpusOggEncoder(),
        AudioFormatEnum.Wav => new WavPassthroughEncoder(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };
}
