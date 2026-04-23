namespace WhisperTray.Core.Orchestration;

public interface INotificationService
{
    void NotifyInfo(string title, string body);
    void NotifyWarning(string title, string body);
    void NotifyError(string title, string body);
}
