using WhisperTray.Core.Configuration;

namespace WhisperTray.Core.Injection;

/// <summary>
/// Combines clipboard + keyboard-simulation primitives into a single injection
/// decision tree based on the user's InjectionMode. Pure logic — all OS calls
/// live behind IClipboardService / ITextTypist / IDelayedExecutor.
/// </summary>
public sealed class CascadingTextInjector : ITextInjector
{
    public static readonly TimeSpan ClipboardRestoreDelay = TimeSpan.FromSeconds(2);

    private readonly IClipboardService _clipboard;
    private readonly ITextTypist _typist;
    private readonly IDelayedExecutor _delay;

    public CascadingTextInjector(IClipboardService clipboard, ITextTypist typist, IDelayedExecutor delay)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(typist);
        ArgumentNullException.ThrowIfNull(delay);
        _clipboard = clipboard;
        _typist = typist;
        _delay = delay;
    }

    public InjectionResult Inject(string text, ForegroundWindowInfo target, InjectionMode mode)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(target);

        if (string.IsNullOrEmpty(text))
        {
            return new InjectionResult(InjectionOutcome.Skipped, "Empty transcription text.");
        }

        var (effectiveMode, reason) = Resolve(mode, target);

        return effectiveMode switch
        {
            InjectionMode.ClipboardOnly => ExecuteClipboardOnly(text, reason),
            InjectionMode.Type => ExecuteType(text, reason),
            InjectionMode.Paste => ExecutePaste(text, reason),
            _ => throw new InvalidOperationException($"Unhandled injection mode {effectiveMode}."),
        };
    }

    private static (InjectionMode mode, string? reason) Resolve(InjectionMode requested, ForegroundWindowInfo target)
    {
        if (requested != InjectionMode.Auto)
        {
            return (requested, null);
        }

        if (target.IsOwnProcess)
        {
            return (InjectionMode.ClipboardOnly, "Foreground window belongs to WhisperTray; copied to clipboard instead.");
        }

        if (target.RequiresElevation)
        {
            return (InjectionMode.ClipboardOnly, "Foreground window runs elevated; copied to clipboard instead.");
        }

        return (InjectionMode.Paste, null);
    }

    private InjectionResult ExecuteClipboardOnly(string text, string? reason)
    {
        _clipboard.SetText(text);
        return new InjectionResult(InjectionOutcome.ClipboardOnly, reason);
    }

    private InjectionResult ExecuteType(string text, string? reason)
    {
        _typist.TypeUnicode(text);
        return new InjectionResult(InjectionOutcome.Typed, reason);
    }

    private InjectionResult ExecutePaste(string text, string? reason)
    {
        var previous = _clipboard.GetText();
        _clipboard.SetText(text);
        _typist.PressPasteShortcut();

        if (previous is not null)
        {
            _delay.Schedule(ClipboardRestoreDelay, () => _clipboard.SetText(previous));
        }

        return new InjectionResult(InjectionOutcome.Pasted, reason);
    }
}
