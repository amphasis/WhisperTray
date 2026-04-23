using System.Diagnostics;
using System.Runtime.InteropServices;
using WhisperTray.Core.Injection;
using static WhisperTray.App.Adapters.NativeMethods;

namespace WhisperTray.App.Adapters;

public sealed class Win32ForegroundWindowService : IForegroundWindowService
{
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;

    public ForegroundWindowInfo Capture()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == 0)
        {
            return new ForegroundWindowInfo(0, null, false, false);
        }

        _ = GetWindowThreadProcessId(hwnd, out var targetPid);
        var processName = TryGetProcessName(targetPid);
        var isOwn = targetPid == _ownProcessId;
        var requiresElevation = !isOwn && TargetIntegrityExceedsOurs(targetPid);

        return new ForegroundWindowInfo(hwnd, processName, isOwn, requiresElevation);
    }

    private static string? TryGetProcessName(uint pid)
    {
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TargetIntegrityExceedsOurs(uint targetPid)
    {
        var ours = GetIntegrityLevel(GetCurrentProcess());
        if (ours is null)
        {
            return false;
        }

        var targetHandle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, targetPid);
        if (targetHandle == 0)
        {
            // Can't open -> often means the target is at a higher integrity level than we are.
            return true;
        }

        try
        {
            var theirs = GetIntegrityLevel(targetHandle);
            return theirs is uint t && t > ours;
        }
        finally
        {
            CloseHandle(targetHandle);
        }
    }

    private static uint? GetIntegrityLevel(nint processHandle)
    {
        if (!OpenProcessToken(processHandle, TOKEN_QUERY, out var token))
        {
            return null;
        }

        try
        {
            _ = GetTokenInformation(token, TokenIntegrityLevel, nint.Zero, 0, out var needed);
            if (needed == 0)
            {
                return null;
            }

            var buffer = Marshal.AllocHGlobal((int)needed);
            try
            {
                if (!GetTokenInformation(token, TokenIntegrityLevel, buffer, needed, out _))
                {
                    return null;
                }

                // TOKEN_MANDATORY_LABEL -> SID_AND_ATTRIBUTES -> Sid pointer at offset 0.
                var sid = Marshal.ReadIntPtr(buffer);
                var count = GetSidSubAuthorityCount(sid);
                if (count <= 0)
                {
                    return null;
                }

                var subPtr = GetSidSubAuthority(sid, (uint)(count - 1));
                var level = (uint)Marshal.ReadInt32(subPtr);
                return level;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseHandle(token);
        }
    }
}
