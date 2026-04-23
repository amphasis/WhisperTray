using FluentAssertions;
using WhisperTray.Core.Audio;

namespace WhisperTray.Core.Tests.Audio;

public class InMemoryFakeRecorderTests
{
    private static readonly RecordedAudio CannedAudio = new(new byte[] { 1, 0, 2, 0 }, 16_000);

    [Fact]
    public void Start_FlipsIsRecordingTrue()
    {
        var sut = new InMemoryFakeRecorder(CannedAudio);

        sut.Start(null);

        sut.IsRecording.Should().BeTrue();
    }

    [Fact]
    public void Start_RecordsDeviceId()
    {
        var sut = new InMemoryFakeRecorder(CannedAudio);

        sut.Start("device-42");

        sut.LastDeviceId.Should().Be("device-42");
    }

    [Fact]
    public void Stop_AfterStart_ReturnsCannedAudio()
    {
        var sut = new InMemoryFakeRecorder(CannedAudio);

        sut.Start(null);
        var result = sut.Stop();

        result.Should().BeSameAs(CannedAudio);
        sut.IsRecording.Should().BeFalse();
    }

    [Fact]
    public void Start_WhileAlreadyRecording_Throws()
    {
        var sut = new InMemoryFakeRecorder(CannedAudio);
        sut.Start(null);

        var act = () => sut.Start(null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Stop_WithoutStart_Throws()
    {
        var sut = new InMemoryFakeRecorder(CannedAudio);

        var act = () => sut.Stop();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartStopCycle_IncrementsCounts()
    {
        var sut = new InMemoryFakeRecorder(CannedAudio);

        sut.Start(null);
        sut.Stop();
        sut.Start(null);
        sut.Stop();

        sut.StartCount.Should().Be(2);
        sut.StopCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_NullAudio_Throws()
    {
        var act = () => new InMemoryFakeRecorder(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
