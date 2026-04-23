namespace WhisperTray.Core.Hotkeys;

/// <summary>
/// A parsed, validated global hotkey: one or more modifiers plus a single base key.
/// Strings are of the form "Ctrl+Alt+Space" (case-insensitive, modifier order flexible,
/// Ctrl/Control, Win/Windows/Meta all accepted).
/// </summary>
public sealed record HotkeyCombo(HotkeyModifiers Modifiers, uint VirtualKey, string KeyName)
{
    public static HotkeyCombo Parse(string input)
    {
        if (!TryParse(input, out var combo, out var error))
        {
            throw new FormatException(error);
        }
        return combo!;
    }

    public static bool TryParse(string input, out HotkeyCombo? result) =>
        TryParse(input, out result, out _);

    public static bool TryParse(string input, out HotkeyCombo? result, out string? error)
    {
        result = null;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Hotkey string is empty.";
            return false;
        }

        var parts = input.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            error = "Hotkey must contain at least one modifier and a base key (e.g. \"Ctrl+Space\").";
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        string? keyToken = null;

        foreach (var part in parts)
        {
            if (TryMatchModifier(part, out var modifier))
            {
                if ((modifiers & modifier) != 0)
                {
                    error = $"Duplicate modifier: {part}.";
                    return false;
                }
                modifiers |= modifier;
            }
            else
            {
                if (keyToken is not null)
                {
                    error = $"Multiple base keys: \"{keyToken}\" and \"{part}\". Only one non-modifier key is allowed.";
                    return false;
                }
                keyToken = part;
            }
        }

        if (modifiers == HotkeyModifiers.None)
        {
            error = "Hotkey must include at least one modifier (Ctrl, Alt, Shift or Win).";
            return false;
        }

        if (keyToken is null)
        {
            error = "Hotkey must include a base key.";
            return false;
        }

        if (!HotkeyKeyMap.TryGetVirtualKey(keyToken, out var vk, out var canonical))
        {
            error = $"Unknown key: \"{keyToken}\".";
            return false;
        }

        result = new HotkeyCombo(modifiers, vk, canonical);
        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Win))
        {
            parts.Add("Win");
        }
        parts.Add(KeyName);
        return string.Join("+", parts);
    }

    private static bool TryMatchModifier(string token, out HotkeyModifiers modifier)
    {
        modifier = token.ToLowerInvariant() switch
        {
            "ctrl" or "control" => HotkeyModifiers.Control,
            "alt" or "menu" => HotkeyModifiers.Alt,
            "shift" => HotkeyModifiers.Shift,
            "win" or "windows" or "meta" or "super" => HotkeyModifiers.Win,
            _ => HotkeyModifiers.None,
        };
        return modifier != HotkeyModifiers.None;
    }
}
