using System.Windows;
using System.Windows.Threading;
using WhisperTray.Core.Injection;

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
        _dispatcher.Invoke(() => Clipboard.ContainsText() ? Clipboard.GetText() : null);

    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _dispatcher.Invoke(() => Clipboard.SetDataObject(text, copy: true));
    }
}
