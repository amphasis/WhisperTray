namespace WhisperTray.Core.Hotkeys;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Control = 1 << 0,
    Alt = 1 << 1,
    Shift = 1 << 2,
    Win = 1 << 3,
}
