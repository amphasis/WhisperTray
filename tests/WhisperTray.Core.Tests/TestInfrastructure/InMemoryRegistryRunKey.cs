using WhisperTray.Core.Configuration;

namespace WhisperTray.Core.Tests.TestInfrastructure;

/// <summary>In-memory IRegistryRunKey for unit tests. Records writes / deletes so tests can assert on them.</summary>
public sealed class InMemoryRegistryRunKey : IRegistryRunKey
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    public List<string> DeletedNames { get; } = new();

    public IReadOnlyDictionary<string, string> Snapshot => _values;

    public string? GetValue(string name) =>
        _values.TryGetValue(name, out var value) ? value : null;

    public void SetValue(string name, string value) =>
        _values[name] = value;

    public void DeleteValue(string name)
    {
        if (_values.Remove(name))
        {
            DeletedNames.Add(name);
        }
    }
}
