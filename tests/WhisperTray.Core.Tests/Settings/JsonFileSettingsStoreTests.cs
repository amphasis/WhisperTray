using FluentAssertions;
using WhisperTray.Core.Configuration;
using WhisperTray.Core.Tests.TestInfrastructure;

namespace WhisperTray.Core.Tests;

public class JsonFileSettingsStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _path;

    public JsonFileSettingsStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "WhisperTrayTests_" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_FileMissing_ReturnsDefaults()
    {
        var store = new JsonFileSettingsStore(_path);

        store.Load().Should().Be(Settings.Default);
    }

    [Fact]
    public void Save_CreatesParentDirectory()
    {
        var deeperPath = Path.Combine(_tempDir, "nested", "deeper", "settings.json");
        var store = new JsonFileSettingsStore(deeperPath);

        store.Save(Settings.Default);

        File.Exists(deeperPath).Should().BeTrue();
    }

    [Fact]
    public void Save_ThenLoad_PreservesAllFields()
    {
        var original = Settings.Default with
        {
            Hotkey = "Ctrl+Shift+D",
            Autostart = true,
            AudioDeviceId = "{0.0.1.00000000}.{fake-guid}",
            Provider = TranscriptionProvider.Lemonfox,
            BaseUrl = "https://api.lemonfox.ai/v1",
            Model = "whisper-1",
            ApiKey = "sk-my-secret",
            Language = "en",
            PromptHint = "technical terminology",
            AudioFormat = AudioFormat.Wav,
            InjectionMode = InjectionMode.ClipboardOnly,
        };
        var store = new JsonFileSettingsStore(_path);

        store.Save(original);
        var loaded = store.Load();

        loaded.Should().Be(original);
    }

    [Fact]
    public void Save_WithoutProtector_WritesApiKeyAsPlaintext()
    {
        var settings = Settings.Default with { ApiKey = "sk-plaintext-secret" };
        var store = new JsonFileSettingsStore(_path);

        store.Save(settings);
        var rawJson = File.ReadAllText(_path);

        rawJson.Should().Contain("sk-plaintext-secret");
        rawJson.Should().Contain("\"apiKey\"");
        rawJson.Should().NotContain("apiKeyProtected");
    }

    [Fact]
    public void Save_WithProtector_WritesApiKeyProtectedAndOmitsPlaintext()
    {
        var settings = Settings.Default with { ApiKey = "sk-my-secret" };
        var store = new JsonFileSettingsStore(_path, new PassthroughSecretProtector());

        store.Save(settings);
        var rawJson = File.ReadAllText(_path);

        rawJson.Should().Contain("\"apiKeyProtected\"");
        rawJson.Should().NotContain("\"apiKey\":");
        rawJson.Should().NotContain("sk-my-secret");
    }

    [Fact]
    public void SaveThenLoad_WithProtector_RoundTripsApiKey()
    {
        var protector = new PassthroughSecretProtector();
        var settings = Settings.Default with { ApiKey = "sk-round-trip" };
        var store = new JsonFileSettingsStore(_path, protector);

        store.Save(settings);
        var loaded = store.Load();

        loaded.ApiKey.Should().Be("sk-round-trip");
    }

    [Fact]
    public void Load_PlaintextApiKey_WithProtector_IsMigratedOnNextSave()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_path, """{"apiKey": "sk-legacy-plaintext"}""");
        var store = new JsonFileSettingsStore(_path, new PassthroughSecretProtector());

        // First load sees the legacy plaintext key.
        var loaded = store.Load();
        loaded.ApiKey.Should().Be("sk-legacy-plaintext");

        // After an explicit Save the plaintext form disappears from disk.
        store.Save(loaded);
        var rawJson = File.ReadAllText(_path);

        rawJson.Should().Contain("\"apiKeyProtected\"");
        rawJson.Should().NotContain("sk-legacy-plaintext");
    }

    [Fact]
    public void Load_UnprotectFailure_FallsBackToPlaintextApiKey()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_path, """{"apiKey": "sk-fallback", "apiKeyProtected": "!!!not-base64!!!"}""");
        var store = new JsonFileSettingsStore(_path, new PassthroughSecretProtector());

        // Protector's Unprotect returns null on bad input; we fall back to plaintext so
        // the user can keep using the app even after a corrupted profile move.
        store.Load().ApiKey.Should().Be("sk-fallback");
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_path, "{ not valid json ");
        var store = new JsonFileSettingsStore(_path);

        store.Load().Should().Be(Settings.Default);
    }

    [Fact]
    public void Load_EmptyFile_ReturnsDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_path, string.Empty);
        var store = new JsonFileSettingsStore(_path);

        store.Load().Should().Be(Settings.Default);
    }

    [Fact]
    public void Load_PartialJson_FillsMissingFromDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_path, """{"hotkey": "Ctrl+D"}""");
        var store = new JsonFileSettingsStore(_path);

        var loaded = store.Load();

        loaded.Hotkey.Should().Be("Ctrl+D");
        loaded.Provider.Should().Be(Settings.Default.Provider);
        loaded.Model.Should().Be(Settings.Default.Model);
    }

    [Fact]
    public void Save_NoApiKey_OmitsApiKeyField()
    {
        var settings = Settings.Default with { ApiKey = null };
        var store = new JsonFileSettingsStore(_path);

        store.Save(settings);
        var rawJson = File.ReadAllText(_path);

        rawJson.Should().NotContain("\"apiKey\"");
    }

    [Fact]
    public void Save_EnumsSerializeAsCamelCaseStrings()
    {
        var settings = Settings.Default with { Provider = TranscriptionProvider.HuggingFace };
        var store = new JsonFileSettingsStore(_path);

        store.Save(settings);
        var rawJson = File.ReadAllText(_path);

        rawJson.Should().Contain("\"huggingFace\"");
    }
}
