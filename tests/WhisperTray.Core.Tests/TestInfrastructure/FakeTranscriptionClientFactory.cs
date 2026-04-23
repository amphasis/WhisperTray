using WhisperTray.Core.Configuration;
using WhisperTray.Core.Orchestration;
using WhisperTray.Core.Transcription;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class FakeTranscriptionClientFactory : ITranscriptionClientFactory
{
    public FakeTranscriptionClient Client { get; }

    public List<Settings> CreatedWith { get; } = new();

    public FakeTranscriptionClientFactory(FakeTranscriptionClient client)
    {
        Client = client;
    }

    public ITranscriptionClient Create(Settings settings)
    {
        CreatedWith.Add(settings);
        return Client;
    }
}
