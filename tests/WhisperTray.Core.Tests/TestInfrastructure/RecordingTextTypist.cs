using WhisperTray.Core.Injection;

namespace WhisperTray.Core.Tests.TestInfrastructure;

public sealed class RecordingTextTypist : ITextTypist
{
    public List<string> TypedSegments { get; } = new();

    public int PasteShortcutCount { get; private set; }

    public void TypeUnicode(string text) => TypedSegments.Add(text);

    public void PressPasteShortcut() => PasteShortcutCount++;
}
