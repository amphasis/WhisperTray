using FluentAssertions;
using WhisperTray.Core.Configuration;

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
    public void Save_WritesApiKeyAsPlaintextForNow()
    {
        // Interim: key is stored as plaintext until Phase 9 rewires ISecretProtector.
        var settings = Settings.Default with { ApiKey = "sk-plaintext-secret" };
        var store = new JsonFileSettingsStore(_path);

        store.Save(settings);
        var rawJson = File.ReadAllText(_path);

        rawJson.Should().Contain("sk-plaintext-secret");
        rawJson.Should().Contain("\"apiKey\"");
        rawJson.Should().NotContain("apiKeyEncrypted");
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
