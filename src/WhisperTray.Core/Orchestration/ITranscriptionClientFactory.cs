using WhisperTray.Core.Configuration;
using WhisperTray.Core.Transcription;

namespace WhisperTray.Core.Orchestration;

public interface ITranscriptionClientFactory
{
    ITranscriptionClient Create(Settings settings);
}
