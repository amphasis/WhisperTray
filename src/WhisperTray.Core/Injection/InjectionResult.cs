namespace WhisperTray.Core.Injection;

public enum InjectionOutcome
{
    Pasted,
    Typed,
    ClipboardOnly,
    Skipped,
}

public sealed record InjectionResult(InjectionOutcome Outcome, string? Reason = null);
