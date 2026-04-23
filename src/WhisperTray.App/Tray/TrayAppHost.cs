using System.Drawing;
using System.Windows.Threading;
using WhisperTray.Core.Orchestration;
using WinFormsContextMenu = System.Windows.Forms.ContextMenuStrip;
using WinFormsMenuItem = System.Windows.Forms.ToolStripMenuItem;
using WinFormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using WinFormsTipIcon = System.Windows.Forms.ToolTipIcon;

namespace WhisperTray.App.Tray;

/// <summary>
/// Owns the tray icon, implements INotificationService via balloon tips, and
/// publishes Settings / Quit menu events for the composition root to handle.
/// All WinForms NotifyIcon interaction is marshalled onto the WPF dispatcher.
/// </summary>
public sealed class TrayAppHost : INotificationService, IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly WinFormsNotifyIcon _icon;
    private bool _disposed;

    public TrayAppHost(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;

        _icon = new WinFormsNotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "WhisperTray — Idle",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
    }

    public event EventHandler? SettingsRequested;

    public event EventHandler? QuitRequested;

    public void UpdateState(OrchestratorState state)
    {
        InvokeOnDispatcher(() =>
        {
            _icon.Text = state switch
            {
                OrchestratorState.Idle => "WhisperTray — Idle",
                OrchestratorState.Recording => "WhisperTray — Recording…",
                OrchestratorState.Transcribing => "WhisperTray — Transcribing…",
                OrchestratorState.Injecting => "WhisperTray — Inserting…",
                _ => "WhisperTray",
            };
        });
    }

    public void NotifyInfo(string title, string body) =>
        ShowBalloon(title, body, WinFormsTipIcon.Info);

    public void NotifyWarning(string title, string body) =>
        ShowBalloon(title, body, WinFormsTipIcon.Warning);

    public void NotifyError(string title, string body) =>
        ShowBalloon(title, body, WinFormsTipIcon.Error);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _icon.Visible = false;
        _icon.Dispose();
        _disposed = true;
    }

    private void ShowBalloon(string title, string body, WinFormsTipIcon icon)
    {
        InvokeOnDispatcher(() => _icon.ShowBalloonTip(3_000, title, body, icon));
    }

    private void InvokeOnDispatcher(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _dispatcher.Invoke(action);
        }
    }

    private WinFormsContextMenu BuildMenu()
    {
        var menu = new WinFormsContextMenu();
        var settings = new WinFormsMenuItem("Settings…");
        settings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        var quit = new WinFormsMenuItem("Quit");
        quit.Click += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(settings);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(quit);
        return menu;
    }
}
