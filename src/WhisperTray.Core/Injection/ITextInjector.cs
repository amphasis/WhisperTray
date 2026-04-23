using WhisperTray.Core.Configuration;

namespace WhisperTray.Core.Injection;

public interface ITextInjector
{
    InjectionResult Inject(string text, ForegroundWindowInfo target, InjectionMode mode);
}
