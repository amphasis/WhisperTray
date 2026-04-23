namespace WhisperTray.Core.Configuration;

/// <summary>
/// Minimal abstraction over a per-user "Run" registry key. Used by
/// <see cref="RegistryAutostartService"/> so the autostart logic stays
/// unit-testable without touching real registry hives.
/// </summary>
public interface IRegistryRunKey
{
    /// <summary>Returns the string value stored under <paramref name="name"/>, or null when absent.</summary>
    string? GetValue(string name);

    /// <summary>Writes <paramref name="value"/> as a REG_SZ entry under <paramref name="name"/>.</summary>
    void SetValue(string name, string value);

    /// <summary>Deletes the value. A no-op if the entry does not exist.</summary>
    void DeleteValue(string name);
}
