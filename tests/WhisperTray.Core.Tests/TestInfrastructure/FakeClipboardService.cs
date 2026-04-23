using WhisperTray.Core.Injection;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class FakeClipboardService : IClipboardService
{
    public string? Text { get; private set; }

    public int SetCount { get; private set; }

    public FakeClipboardService(string? initialText = null)
    {
        Text = initialText;
    }

    public string? GetText() => Text;

    public void SetText(string text)
    {
        Text = text;
        SetCount++;
    }
}
