namespace WhisperTray.Core.Injection;

/// <summary>
/// Schedules a callback after a delay. Abstracted so tests can trigger the
/// clipboard-restore step without waiting in real time.
/// </summary>
public interface IDelayedExecutor
{
    void Schedule(TimeSpan delay, Action action);
}
