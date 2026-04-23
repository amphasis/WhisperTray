using WhisperTray.Core.Injection;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class ManualDelayedExecutor : IDelayedExecutor
{
    public List<(TimeSpan Delay, Action Action)> Scheduled { get; } = new();

    public void Schedule(TimeSpan delay, Action action)
    {
        Scheduled.Add((delay, action));
    }

    public void FireAll()
    {
        foreach (var (_, action) in Scheduled)
        {
            action();
        }
        Scheduled.Clear();
    }
}
