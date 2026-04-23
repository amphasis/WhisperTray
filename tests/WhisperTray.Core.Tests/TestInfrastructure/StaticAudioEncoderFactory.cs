using WhisperTray.Core.Audio;
using WhisperTray.Core.Orchestration;
using AudioFormatEnum = WhisperTray.Core.Configuration.AudioFormat;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class StaticAudioEncoderFactory : IAudioEncoderFactory
{
    private readonly IAudioEncoder _encoder;

    public List<AudioFormatEnum> RequestedFormats { get; } = new();

    public StaticAudioEncoderFactory(IAudioEncoder encoder)
    {
        _encoder = encoder;
    }

    public IAudioEncoder Create(AudioFormatEnum format)
    {
        RequestedFormats.Add(format);
        return _encoder;
    }
}
