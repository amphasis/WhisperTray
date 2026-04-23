using System.Runtime.Versioning;
using Microsoft.Win32;
using WhisperTray.Core.Configuration;

namespace WhisperTray.App.Adapters;

/// <summary>
/// Concrete <see cref="IRegistryRunKey"/> backed by
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>. That hive is writable
/// without elevation and is the standard location for per-user autostart entries.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class HkcuRunRegistryKey : IRegistryRunKey
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public string? GetValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(name) as string;
    }

    public void SetValue(string name, string value)
    {
        // CreateSubKey returns the existing key if present, or creates it.
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException($"Cannot open or create HKCU\\{RunKeyPath}.");
        key.SetValue(name, value, RegistryValueKind.String);
    }

    public void DeleteValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
