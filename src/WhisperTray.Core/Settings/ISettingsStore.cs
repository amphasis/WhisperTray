namespace WhisperTray.Core.Configuration;

public interface ISettingsStore
{
    Settings Load();
    void Save(Settings settings);
}
