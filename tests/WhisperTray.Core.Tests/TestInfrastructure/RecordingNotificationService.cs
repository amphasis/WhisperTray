using WhisperTray.Core.Orchestration;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class RecordingNotificationService : INotificationService
{
    public enum Severity { Info, Warning, Error }

    public List<(Severity Severity, string Title, string Body)> Notifications { get; } = new();

    public void NotifyInfo(string title, string body) =>
        Notifications.Add((Severity.Info, title, body));

    public void NotifyWarning(string title, string body) =>
        Notifications.Add((Severity.Warning, title, body));

    public void NotifyError(string title, string body) =>
        Notifications.Add((Severity.Error, title, body));
}
