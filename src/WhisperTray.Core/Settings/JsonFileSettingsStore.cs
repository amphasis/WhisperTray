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
    private readonly ISecretProtector _protector;

    public JsonFileSettingsStore(string path, ISecretProtector protector)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(protector);
        _path = path;
        _protector = protector;
    }

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

    private Settings FromPersisted(PersistedSettings p)
    {
        string? apiKey = null;
        if (!string.IsNullOrEmpty(p.ApiKeyEncrypted))
        {
            apiKey = _protector.Unprotect(p.ApiKeyEncrypted);
        }

        return new Settings
        {
            Hotkey = p.Hotkey ?? Settings.Default.Hotkey,
            Autostart = p.Autostart ?? Settings.Default.Autostart,
            AudioDeviceId = p.AudioDeviceId,
            Provider = p.Provider ?? Settings.Default.Provider,
            BaseUrl = p.BaseUrl ?? Settings.Default.BaseUrl,
            Model = p.Model ?? Settings.Default.Model,
            ApiKey = apiKey,
            Language = p.Language,
            PromptHint = p.PromptHint ?? Settings.Default.PromptHint,
            AudioFormat = p.AudioFormat ?? Settings.Default.AudioFormat,
            InjectionMode = p.InjectionMode ?? Settings.Default.InjectionMode,
        };
    }

    private PersistedSettings ToPersisted(Settings s)
    {
        string? apiKeyEncrypted = null;
        if (!string.IsNullOrEmpty(s.ApiKey))
        {
            apiKeyEncrypted = _protector.Protect(s.ApiKey);
        }

        return new PersistedSettings
        {
            Hotkey = s.Hotkey,
            Autostart = s.Autostart,
            AudioDeviceId = s.AudioDeviceId,
            Provider = s.Provider,
            BaseUrl = s.BaseUrl,
            Model = s.Model,
            ApiKeyEncrypted = apiKeyEncrypted,
            Language = s.Language,
            PromptHint = s.PromptHint,
            AudioFormat = s.AudioFormat,
            InjectionMode = s.InjectionMode,
        };
    }

    private sealed record PersistedSettings
    {
        public string? Hotkey { get; init; }
        public bool? Autostart { get; init; }
        public string? AudioDeviceId { get; init; }
        public TranscriptionProvider? Provider { get; init; }
        public string? BaseUrl { get; init; }
        public string? Model { get; init; }
        public string? ApiKeyEncrypted { get; init; }
        public string? Language { get; init; }
        public string? PromptHint { get; init; }
        public AudioFormat? AudioFormat { get; init; }
        public InjectionMode? InjectionMode { get; init; }
    }
}
