namespace WhisperTray.Core.Injection;

public interface IForegroundWindowService
{
    /// <summary>
    /// Reads the current foreground window. Must be called before showing any
    /// of our own UI, otherwise the captured target becomes our own balloon/menu.
    /// </summary>
    ForegroundWindowInfo Capture();
}
