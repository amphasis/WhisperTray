namespace WhisperTray.Core.Injection;

/// <summary>Low-level keyboard simulation primitives — implemented via SendInput on Windows.</summary>
public interface ITextTypist
{
    /// <summary>Types the given text via simulated Unicode key events.</summary>
    void TypeUnicode(string text);

    /// <summary>Presses Ctrl+V to trigger paste in the focused window.</summary>
    void PressPasteShortcut();
}
