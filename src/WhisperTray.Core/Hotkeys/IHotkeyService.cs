namespace WhisperTray.Core.Hotkeys;

public interface IHotkeyService
{
    /// <summary>Fired on the UI dispatcher thread when the registered combo is pressed.</summary>
    event EventHandler? Toggled;

    /// <summary>Registers the combo. Replaces any previous registration.</summary>
    void Register(HotkeyCombo combo);

    /// <summary>Removes any active registration. Safe to call when nothing is registered.</summary>
    void Unregister();
}
