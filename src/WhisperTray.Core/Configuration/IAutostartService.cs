namespace WhisperTray.Core.Configuration;

/// <summary>
/// Enables / disables the app launching when the user signs in.
/// On Windows this is implemented via the per-user Run key in the registry;
/// the Core abstraction lets us unit-test the logic without touching HKCU.
/// </summary>
public interface IAutostartService
{
    /// <summary>Returns true if an autostart entry for this app is currently present.</summary>
    bool IsEnabled();

    /// <summary>Installs or refreshes the autostart entry pointing at <paramref name="executablePath"/>.</summary>
    void Enable(string executablePath);

    /// <summary>Removes the autostart entry (no-op if none exists).</summary>
    void Disable();
}
