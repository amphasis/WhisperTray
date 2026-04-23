using System.Windows.Threading;
using WhisperTray.Core.Injection;
using WpfClipboard = System.Windows.Clipboard;

namespace WhisperTray.App.Adapters;

public sealed class WpfClipboardService : IClipboardService
{
    private readonly Dispatcher _dispatcher;

    public WpfClipboardService(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    public string? GetText() =>
        _dispatcher.Invoke(() => WpfClipboard.ContainsText() ? WpfClipboard.GetText() : null);

    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _dispatcher.Invoke(() => WpfClipboard.SetDataObject(text, copy: true));
    }
}
