namespace WhisperTray.Core.Audio;

/// <summary>Deterministic test double — returns a preloaded RecordedAudio on Stop.</summary>
public sealed class InMemoryFakeRecorder : IAudioRecorder
{
    private readonly RecordedAudio _audioToReturn;

    public InMemoryFakeRecorder(RecordedAudio audioToReturn)
    {
        ArgumentNullException.ThrowIfNull(audioToReturn);
        _audioToReturn = audioToReturn;
    }

    public bool IsRecording { get; private set; }

    public string? LastDeviceId { get; private set; }

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public void Start(string? deviceId)
    {
        if (IsRecording)
        {
            throw new InvalidOperationException("Recorder is already recording.");
        }

        IsRecording = true;
        LastDeviceId = deviceId;
        StartCount++;
    }

    public RecordedAudio Stop()
    {
        if (!IsRecording)
        {
            throw new InvalidOperationException("Recorder is not currently recording.");
        }

        IsRecording = false;
        StopCount++;
        return _audioToReturn;
    }
}
