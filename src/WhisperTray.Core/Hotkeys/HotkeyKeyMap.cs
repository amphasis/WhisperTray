namespace WhisperTray.Core.Hotkeys;

/// <summary>
/// Maps human-readable key names (case-insensitive, with aliases) to Win32 virtual
/// key codes and to a canonical name used when serializing a combo back to string.
/// </summary>
internal static class HotkeyKeyMap
{
    private static readonly Dictionary<string, (uint Vk, string Canonical)> Map = BuildMap();

    public static bool TryGetVirtualKey(string token, out uint virtualKey, out string canonicalName)
    {
        if (Map.TryGetValue(token.ToLowerInvariant(), out var entry))
        {
            virtualKey = entry.Vk;
            canonicalName = entry.Canonical;
            return true;
        }

        virtualKey = 0;
        canonicalName = string.Empty;
        return false;
    }

    private static Dictionary<string, (uint Vk, string Canonical)> BuildMap()
    {
        var map = new Dictionary<string, (uint, string)>(StringComparer.OrdinalIgnoreCase);

        // Letters A-Z
        for (var c = 'A'; c <= 'Z'; c++)
        {
            var name = c.ToString();
            map[name.ToLowerInvariant()] = ((uint)c, name);
        }

        // Digits 0-9 (top row)
        for (var c = '0'; c <= '9'; c++)
        {
            var name = c.ToString();
            map[name] = ((uint)c, name);
        }

        // Function keys F1..F24 (VK_F1 = 0x70)
        for (var i = 1; i <= 24; i++)
        {
            var name = "F" + i;
            map[name.ToLowerInvariant()] = ((uint)(0x70 + i - 1), name);
        }

        // Named keys
        void Add(string canonical, uint vk, params string[] aliases)
        {
            map[canonical.ToLowerInvariant()] = (vk, canonical);
            foreach (var alias in aliases)
            {
                map[alias.ToLowerInvariant()] = (vk, canonical);
            }
        }

        Add("Space", 0x20);
        Add("Escape", 0x1B, "Esc");
        Add("Tab", 0x09);
        Add("Enter", 0x0D, "Return");
        Add("Backspace", 0x08);
        Add("Insert", 0x2D, "Ins");
        Add("Delete", 0x2E, "Del");
        Add("Home", 0x24);
        Add("End", 0x23);
        Add("PageUp", 0x21, "PgUp");
        Add("PageDown", 0x22, "PgDn");
        Add("Left", 0x25);
        Add("Right", 0x27);
        Add("Up", 0x26);
        Add("Down", 0x28);
        Add("PrintScreen", 0x2C, "PrtSc");
        Add("Pause", 0x13);
        Add("CapsLock", 0x14);
        Add("NumLock", 0x90);
        Add("ScrollLock", 0x91);

        // Numpad
        for (var i = 0; i <= 9; i++)
        {
            var name = "Numpad" + i;
            map[name.ToLowerInvariant()] = ((uint)(0x60 + i), name);
        }
        Add("NumpadMultiply", 0x6A, "NumpadStar");
        Add("NumpadAdd", 0x6B, "NumpadPlus");
        Add("NumpadSubtract", 0x6D, "NumpadMinus");
        Add("NumpadDecimal", 0x6E);
        Add("NumpadDivide", 0x6F, "NumpadSlash");

        // OEM punctuation (US layout; names stable across layouts)
        Add("Semicolon", 0xBA, ";");
        Add("Equals", 0xBB, "=");
        Add("Comma", 0xBC, ",");
        Add("Minus", 0xBD, "-");
        Add("Period", 0xBE, ".");
        Add("Slash", 0xBF, "/");
        Add("Backtick", 0xC0, "`", "Tilde");
        Add("LeftBracket", 0xDB, "[");
        Add("Backslash", 0xDC, @"\");
        Add("RightBracket", 0xDD, "]");
        Add("Quote", 0xDE, "'");

        return map;
    }
}
