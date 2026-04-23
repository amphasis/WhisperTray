using WhisperTray.Core.Injection;

namespace WhisperTray.App.Adapters;

public sealed class TaskDelayedExecutor : IDelayedExecutor
{
    public void Schedule(TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _ = Task.Run(async () =>
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }
            try
            {
                action();
            }
            catch
            {
                // Swallowing here is deliberate: a failure in the scheduled
                // restore callback (e.g., clipboard contention) shouldn't crash
                // the whole app. The user keeps their pasted text either way.
            }
        });
    }
}
