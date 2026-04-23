using System.ComponentModel;
using System.Runtime.InteropServices;
using WhisperTray.Core.Injection;
using static WhisperTray.App.Adapters.NativeMethods;

namespace WhisperTray.App.Adapters;

public sealed class SendInputTextTypist : ITextTypist
{
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    public void TypeUnicode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return;
        }

        // Each UTF-16 code unit produces a keydown + keyup pair.
        var inputs = new INPUT[text.Length * 2];
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            inputs[i * 2] = MakeUnicodeInput(ch, keyUp: false);
            inputs[i * 2 + 1] = MakeUnicodeInput(ch, keyUp: true);
        }

        SendOrThrow(inputs);
    }

    public void PressPasteShortcut()
    {
        var inputs = new[]
        {
            MakeVkInput(VK_CONTROL, keyUp: false),
            MakeVkInput(VK_V, keyUp: false),
            MakeVkInput(VK_V, keyUp: true),
            MakeVkInput(VK_CONTROL, keyUp: true),
        };
        SendOrThrow(inputs);
    }

    private static void SendOrThrow(INPUT[] inputs)
    {
        var sent = SendInput((uint)inputs.Length, inputs, InputSize);
        if (sent != (uint)inputs.Length)
        {
            var code = Marshal.GetLastWin32Error();
            throw new Win32Exception(code,
                $"SendInput dispatched {sent}/{inputs.Length} events (cbSize={InputSize}, lastError=0x{code:X}).");
        }
    }

    private static INPUT MakeUnicodeInput(char ch, bool keyUp) => new()
    {
        Type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            Ki = new KEYBDINPUT
            {
                WVk = 0,
                WScan = ch,
                DwFlags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0u),
                Time = 0,
                DwExtraInfo = nint.Zero,
            },
        },
    };

    private static INPUT MakeVkInput(ushort vk, bool keyUp) => new()
    {
        Type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            Ki = new KEYBDINPUT
            {
                WVk = vk,
                WScan = 0,
                DwFlags = keyUp ? KEYEVENTF_KEYUP : 0u,
                Time = 0,
                DwExtraInfo = nint.Zero,
            },
        },
    };
}
