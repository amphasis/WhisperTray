using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhisperTray.Core.Configuration;

public sealed class JsonFileSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;

    public JsonFileSettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _path = path;
    }

    // NOTE: the API key is currently persisted in plaintext. ISecretProtector /
    // DpapiSecretProtector stay in the codebase and will be re-wired here once
    // the Settings window (Phase 9) can drive the save/migrate flow properly.

    public Settings Load()
    {
        if (!File.Exists(_path))
        {
            return Settings.Default;
        }

        string json;
        try
        {
            json = File.ReadAllText(_path);
        }
        catch (IOException)
        {
            return Settings.Default;
        }

        PersistedSettings? persisted;
        try
        {
            persisted = JsonSerializer.Deserialize<PersistedSettings>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return Settings.Default;
        }

        return persisted is null
            ? Settings.Default
            : FromPersisted(persisted);
    }

    public void Save(Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var persisted = ToPersisted(settings);
        var json = JsonSerializer.Serialize(persisted, JsonOptions);
        File.WriteAllText(_path, json);
    }

    private static Settings FromPersisted(PersistedSettings p) => new()
    {
        Hotkey = p.Hotkey ?? Settings.Default.Hotkey,
        Autostart = p.Autostart ?? Settings.Default.Autostart,
        AudioDeviceId = p.AudioDeviceId,
        Provider = p.Provider ?? Settings.Default.Provider,
        BaseUrl = p.BaseUrl ?? Settings.Default.BaseUrl,
        Model = p.Model ?? Settings.Default.Model,
        ApiKey = p.ApiKey,
        Language = p.Language,
        PromptHint = p.PromptHint ?? Settings.Default.PromptHint,
        AudioFormat = p.AudioFormat ?? Settings.Default.AudioFormat,
        InjectionMode = p.InjectionMode ?? Settings.Default.InjectionMode,
    };

    private static PersistedSettings ToPersisted(Settings s) => new()
    {
        Hotkey = s.Hotkey,
        Autostart = s.Autostart,
        AudioDeviceId = s.AudioDeviceId,
        Provider = s.Provider,
        BaseUrl = s.BaseUrl,
        Model = s.Model,
        ApiKey = s.ApiKey,
        Language = s.Language,
        PromptHint = s.PromptHint,
        AudioFormat = s.AudioFormat,
        InjectionMode = s.InjectionMode,
    };

    private sealed record PersistedSettings
    {
        public string? Hotkey { get; init; }
        public bool? Autostart { get; init; }
        public string? AudioDeviceId { get; init; }
        public TranscriptionProvider? Provider { get; init; }
        public string? BaseUrl { get; init; }
        public string? Model { get; init; }
        public string? ApiKey { get; init; }
        public string? Language { get; init; }
        public string? PromptHint { get; init; }
        public AudioFormat? AudioFormat { get; init; }
        public InjectionMode? InjectionMode { get; init; }
    }
}
