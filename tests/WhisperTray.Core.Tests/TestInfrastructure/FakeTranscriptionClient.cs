using WhisperTray.Core.Transcription;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class FakeTranscriptionClient : ITranscriptionClient
{
    public TranscriptionResult ResultToReturn { get; set; } = new("fake transcription");

    public Exception? ExceptionToThrow { get; set; }

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public List<TranscriptionRequest> RequestsReceived { get; } = new();

    public async Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request, CancellationToken cancellationToken = default)
    {
        RequestsReceived.Add(request);
        if (Delay > TimeSpan.Zero)
        {
            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
        }
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
        return ResultToReturn;
    }
}
