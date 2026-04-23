using FluentAssertions;
using WhisperTray.Core.Configuration;
using WhisperTray.Core.Tests.TestInfrastructure;

namespace WhisperTray.Core.Tests;

public class RegistryAutostartServiceTests
{
    [Fact]
    public void IsEnabled_NoEntry_ReturnsFalse()
    {
        var runKey = new InMemoryRegistryRunKey();
        var svc = new RegistryAutostartService(runKey);

        svc.IsEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_EntryPresent_ReturnsTrue()
    {
        var runKey = new InMemoryRegistryRunKey();
        runKey.SetValue("WhisperTray", "\"C:\\App\\WhisperTray.exe\"");
        var svc = new RegistryAutostartService(runKey);

        svc.IsEnabled().Should().BeTrue();
    }

    [Fact]
    public void Enable_WritesQuotedExecutablePath()
    {
        var runKey = new InMemoryRegistryRunKey();
        var svc = new RegistryAutostartService(runKey);

        svc.Enable(@"C:\Program Files\WhisperTray\WhisperTray.App.exe");

        runKey.GetValue("WhisperTray")
            .Should().Be("\"C:\\Program Files\\WhisperTray\\WhisperTray.App.exe\"");
    }

    [Fact]
    public void Enable_AlreadyQuotedPath_DoesNotDoubleQuote()
    {
        var runKey = new InMemoryRegistryRunKey();
        var svc = new RegistryAutostartService(runKey);

        svc.Enable("\"C:\\App\\WhisperTray.App.exe\"");

        runKey.GetValue("WhisperTray").Should().Be("\"C:\\App\\WhisperTray.App.exe\"");
    }

    [Fact]
    public void Enable_OverwritesStaleEntry()
    {
        var runKey = new InMemoryRegistryRunKey();
        runKey.SetValue("WhisperTray", "\"C:\\Old\\Location.exe\"");
        var svc = new RegistryAutostartService(runKey);

        svc.Enable(@"C:\New\WhisperTray.App.exe");

        runKey.GetValue("WhisperTray").Should().Be("\"C:\\New\\WhisperTray.App.exe\"");
    }

    [Fact]
    public void Disable_RemovesEntry()
    {
        var runKey = new InMemoryRegistryRunKey();
        runKey.SetValue("WhisperTray", "\"C:\\App.exe\"");
        var svc = new RegistryAutostartService(runKey);

        svc.Disable();

        svc.IsEnabled().Should().BeFalse();
        runKey.DeletedNames.Should().Contain("WhisperTray");
    }

    [Fact]
    public void Disable_WhenAlreadyAbsent_IsNoOp()
    {
        var runKey = new InMemoryRegistryRunKey();
        var svc = new RegistryAutostartService(runKey);

        var act = () => svc.Disable();

        act.Should().NotThrow();
    }

    [Fact]
    public void CustomValueName_IsRespected()
    {
        var runKey = new InMemoryRegistryRunKey();
        var svc = new RegistryAutostartService(runKey, valueName: "WhisperTrayDev");

        svc.Enable(@"C:\App.exe");

        runKey.GetValue("WhisperTrayDev").Should().NotBeNull();
        runKey.GetValue("WhisperTray").Should().BeNull();
    }
}
