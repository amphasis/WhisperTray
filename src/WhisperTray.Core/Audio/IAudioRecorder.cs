namespace WhisperTray.Core.Audio;

public interface IAudioRecorder
{
    bool IsRecording { get; }

    /// <summary>Begins capturing from the given device (null = system default).</summary>
    void Start(string? deviceId);

    /// <summary>Stops capture and returns everything buffered since Start.</summary>
    RecordedAudio Stop();
}
