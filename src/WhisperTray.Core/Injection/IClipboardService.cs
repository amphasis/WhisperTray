namespace WhisperTray.Core.Injection;

public interface IClipboardService
{
    /// <summary>Current clipboard text, or null if the clipboard is empty / not text.</summary>
    string? GetText();

    void SetText(string text);
}
