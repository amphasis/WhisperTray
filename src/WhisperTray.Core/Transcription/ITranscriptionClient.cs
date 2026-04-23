namespace WhisperTray.Core.Transcription;

public interface ITranscriptionClient
{
    Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request, CancellationToken cancellationToken = default);
}
