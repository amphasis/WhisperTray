namespace WhisperTray.Core.Injection;

/// <summary>Snapshot of the foreground window at the moment transcription was requested.</summary>
public sealed record ForegroundWindowInfo(
    nint WindowHandle,
    string? ProcessName,
    bool IsOwnProcess,
    bool RequiresElevation);
