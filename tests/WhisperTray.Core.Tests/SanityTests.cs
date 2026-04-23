using FluentAssertions;

namespace WhisperTray.Core.Tests;

public class SanityTests
{
    [Fact]
    public void TestRunner_Runs_TrueIsTrue()
    {
        true.Should().BeTrue();
    }
}
