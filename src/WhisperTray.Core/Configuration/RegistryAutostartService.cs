namespace WhisperTray.Core.Configuration;

/// <summary>
/// Registers the app under a "Run" registry key so it starts at sign-in.
/// The path is quoted to survive executables that live in directories with
/// spaces (e.g. Program Files). On every Enable we overwrite the existing
/// value — that repairs stale entries if the app has been moved.
/// </summary>
public sealed class RegistryAutostartService : IAutostartService
{
    /// <summary>Default value name under the Run key. Shared by install/uninstall paths.</summary>
    public const string DefaultValueName = "WhisperTray";

    private readonly IRegistryRunKey _runKey;
    private readonly string _valueName;

    public RegistryAutostartService(IRegistryRunKey runKey, string valueName = DefaultValueName)
    {
        ArgumentNullException.ThrowIfNull(runKey);
        ArgumentException.ThrowIfNullOrEmpty(valueName);
        _runKey = runKey;
        _valueName = valueName;
    }

    public bool IsEnabled() => !string.IsNullOrEmpty(_runKey.GetValue(_valueName));

    public void Enable(string executablePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(executablePath);
        _runKey.SetValue(_valueName, QuoteIfNeeded(executablePath));
    }

    public void Disable() => _runKey.DeleteValue(_valueName);

    private static string QuoteIfNeeded(string path) =>
        path.StartsWith('"') ? path : $"\"{path}\"";
}
