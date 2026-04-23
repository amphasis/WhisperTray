using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using WhisperTray.Core.Hotkeys;
using static WhisperTray.App.Adapters.NativeMethods;

namespace WhisperTray.App.Adapters;

/// <summary>
/// Global hotkey via WH_KEYBOARD_LL. The hook callback runs on the thread that
/// installs it (here, the WPF UI thread). Matching keystrokes are swallowed so
/// the combo does not leak into whatever app has focus.
/// </summary>
public sealed class GlobalHotkeyService : IHotkeyService, IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly LowLevelKeyboardProc _procDelegate;
    private nint _hookHandle;
    private HotkeyCombo? _registered;
    private bool _disposed;

    public GlobalHotkeyService(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
        // Pin the delegate so the unmanaged side never calls a collected closure.
        _procDelegate = HookProc;
    }

    public event EventHandler? Toggled;

    public void Register(HotkeyCombo combo)
    {
        ArgumentNullException.ThrowIfNull(combo);
        ThrowIfDisposed();

        _registered = combo;
        if (_hookHandle == 0)
        {
            var module = GetModuleHandle(null);
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _procDelegate, module, 0);
            if (_hookHandle == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install low-level keyboard hook.");
            }
        }
    }

    public void Unregister()
    {
        _registered = null;
        if (_hookHandle != 0)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Unregister();
        _disposed = true;
    }

    private nint HookProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0 || _registered is null)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var msg = wParam.ToInt32();
        if (msg is not (WM_KEYDOWN or WM_SYSKEYDOWN))
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        if (data.VkCode != _registered.VirtualKey)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (CurrentModifiers() != _registered.Modifiers)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        _dispatcher.BeginInvoke(() => Toggled?.Invoke(this, EventArgs.Empty));
        return 1; // swallow the key so it doesn't leak into the focused app
    }

    private static HotkeyModifiers CurrentModifiers()
    {
        var mods = HotkeyModifiers.None;
        if (IsKeyDown(VK_CONTROL))
        {
            mods |= HotkeyModifiers.Control;
        }
        if (IsKeyDown(VK_MENU))
        {
            mods |= HotkeyModifiers.Alt;
        }
        if (IsKeyDown(VK_SHIFT))
        {
            mods |= HotkeyModifiers.Shift;
        }
        if (IsKeyDown(VK_LWIN) || IsKeyDown(VK_RWIN))
        {
            mods |= HotkeyModifiers.Win;
        }
        return mods;
    }

    private static bool IsKeyDown(ushort vk) => (GetKeyState(vk) & 0x8000) != 0;

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
