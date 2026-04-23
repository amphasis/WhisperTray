using System.IO;
using System.Net.Http;
using System.Windows.Threading;
using WhisperTray.App.Adapters;
using WhisperTray.App.Tray;
using WhisperTray.Core.Configuration;
using WhisperTray.Core.Hotkeys;
using WhisperTray.Core.Injection;
using WhisperTray.Core.Orchestration;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace WhisperTray.App;

/// <summary>
/// Composes every service in the app. Lives for the duration of the process,
/// disposed on App.OnExit. Mutable current-settings snapshot is updated when
/// the Settings window saves (Phase 9); for now Settings are read from disk
/// once at startup.
/// </summary>
public sealed class CompositionRoot : IDisposable
{
    private readonly HttpClient _http;
    private readonly GlobalHotkeyService _hotkey;
    private readonly NAudioRecorder _recorder;
    private readonly TrayAppHost _tray;
    private readonly Orchestrator _orchestrator;
    private readonly ISettingsStore _settingsStore;
    private readonly Settings _currentSettings;

    public CompositionRoot(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WhisperTray",
            "settings.json");
        // ApiKey is stored as plaintext for now; DpapiSecretProtector stays in
        // the codebase and will be wired back once Phase 9 can drive migration.
        _settingsStore = new JsonFileSettingsStore(settingsPath);
        _currentSettings = _settingsStore.Load();

        _recorder = new NAudioRecorder();
        var encoderFactory = new DefaultAudioEncoderFactory();
        var clientFactory = new HttpTranscriptionClientFactory(_http);
        var clipboard = new WpfClipboardService(dispatcher);
        var typist = new SendInputTextTypist();
        var delayExecutor = new TaskDelayedExecutor();
        var injector = new CascadingTextInjector(clipboard, typist, delayExecutor);
        var foreground = new Win32ForegroundWindowService();

        _tray = new TrayAppHost(dispatcher);
        _hotkey = new GlobalHotkeyService(dispatcher);

        _orchestrator = new Orchestrator(
            _recorder,
            encoderFactory,
            clientFactory,
            injector,
            foreground,
            _tray,
            () => _currentSettings);

        _orchestrator.StateChanged += (_, state) => _tray.UpdateState(state);
        _hotkey.Toggled += OnHotkeyToggled;
        _tray.SettingsRequested += OnSettingsRequested;
        _tray.QuitRequested += (_, _) => WpfApplication.Current.Shutdown();
    }

    public void Start()
    {
        if (HotkeyCombo.TryParse(_currentSettings.Hotkey, out var combo, out var hotkeyError) && combo is not null)
        {
            try
            {
                _hotkey.Register(combo);
            }
            catch (Exception ex)
            {
                _tray.NotifyError("Hotkey registration failed", ex.Message);
            }
        }
        else
        {
            _tray.NotifyWarning("Invalid hotkey", hotkeyError ?? "Hotkey could not be parsed.");
        }

        if (string.IsNullOrWhiteSpace(_currentSettings.ApiKey))
        {
            _tray.NotifyInfo(
                "Configure API key",
                "Right-click the tray icon and open Settings to finish setup.");
        }
    }

    public void Dispose()
    {
        _hotkey.Toggled -= OnHotkeyToggled;
        _hotkey.Dispose();
        _recorder.Dispose();
        _tray.Dispose();
        _http.Dispose();
    }

    private async void OnHotkeyToggled(object? sender, EventArgs e)
    {
        try
        {
            await _orchestrator.ToggleAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _tray.NotifyError("Unexpected failure", ex.Message);
        }
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        // Phase 9 will replace this with a real settings window.
        WpfMessageBox.Show(
            "Settings UI ships in Phase 9. Edit %APPDATA%\\WhisperTray\\settings.json manually for now.",
            "WhisperTray",
            WpfMessageBoxButton.OK,
            WpfMessageBoxImage.Information);
    }
}
