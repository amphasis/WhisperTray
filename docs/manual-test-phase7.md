# Manual test checklist — Phase 7 (OS adapters)

Adapters live in `src/WhisperTray.App/Adapters/`. They cannot be unit-tested
(real Win32 / NAudio / DPAPI). Each item below is a single end-to-end check
to run before merging Phase 7 and before each subsequent release.

> These tests assume a working demo host (wired in Phase 8). Until then, run
> them from a tiny throwaway Program.Main or via the interactive C# REPL
> against `WhisperTray.App`.

## GlobalHotkeyService — `Adapters/GlobalHotkeyService.cs`

- [ ] Register `Ctrl+Alt+Space`. Press it in Notepad → `Toggled` fires,
      Notepad does not receive a space character (the key is swallowed).
- [ ] Unregister. Press the combo again → no event, Notepad receives the
      space. Hook handle is released.
- [ ] Register a different combo after unregister → works.
- [ ] Press an unrelated combo (e.g. `Ctrl+Alt+X`) → no event.
- [ ] Hold `Ctrl+Alt`, tap `Space` three times → three events. No stuck
      modifier state.
- [ ] Dispose the service → `UnhookWindowsHookEx` called; process exits
      cleanly.

## SendInputTextTypist — `Adapters/SendInputTextTypist.cs`

- [ ] `TypeUnicode("hello")` in Notepad → appears verbatim.
- [ ] `TypeUnicode("Привет, мир")` in Notepad → Cyrillic appears correctly.
- [ ] `TypeUnicode("emoji 🎙️")` → two surrogate halves emitted; Notepad
      may show ?? depending on font, but WordPad / Chrome address bar
      shows the emoji.
- [ ] `PressPasteShortcut()` after `Clipboard.SetText("abc")` in Notepad
      → `abc` appears.

## WpfClipboardService — `Adapters/WpfClipboardService.cs`

- [ ] `SetText("abc")` from a background thread → Clipboard holds "abc".
- [ ] `GetText()` when clipboard holds an image → returns `null`, no
      exception.
- [ ] `GetText()` right after `SetText` → returns the set text.

## Win32ForegroundWindowService — `Adapters/Win32ForegroundWindowService.cs`

- [ ] Notepad open and focused → `Capture().ProcessName == "notepad"`,
      `IsOwnProcess == false`, `RequiresElevation == false`.
- [ ] Elevated cmd (Run as administrator) focused → `RequiresElevation
      == true` (assuming our app is not elevated).
- [ ] Our own Settings window focused → `IsOwnProcess == true`.
- [ ] Foreground process exits between `Capture` and property access →
      `ProcessName` is null instead of throwing.

## NAudioRecorder + WaveInDeviceEnumerator

- [ ] `List()` returns at least one device on a machine with a mic.
- [ ] `Start(null)` uses the default device. Speak for 2s, `Stop()` returns
      a `RecordedAudio` with ~64 KB (16 kHz × 16-bit × 2s) of PCM.
- [ ] Encode the result with `OpusOggEncoder` and save to disk; play in
      VLC → your voice is audible.
- [ ] `Start(nonexistentDeviceId)` falls back to `-1` (default device) —
      does not throw. Recording still works.
- [ ] `Start` twice without `Stop` → `InvalidOperationException`.
- [ ] `Stop` without `Start` → `InvalidOperationException`.
- [ ] Unplug the mic mid-recording → `RecordingStopped` debug log fires.
- [ ] `Dispose` while recording → stops cleanly, no crash on process exit.

## DpapiSecretProtector

- [ ] `Protect("sk-test")` → non-empty base64 string, not equal to the
      input.
- [ ] `Unprotect(Protect(x))` → `x` (for several values including empty
      string and Unicode).
- [ ] Copy the encrypted blob to another user account on the same machine,
      `Unprotect` → returns `null`, no exception.
- [ ] `Unprotect("garbage")` → returns `null`.

## TaskDelayedExecutor

- [ ] `Schedule(TimeSpan.FromMilliseconds(200), () => { ... })` — action
      runs after the delay, not synchronously.
- [ ] Scheduling an action that throws → app does not crash; exception
      swallowed (by design, see class comment).

## HttpTranscriptionClientFactory

- [ ] With `Settings.ApiKey = null` → `Create()` throws
      `InvalidOperationException` with a sensible message.
- [ ] With a real OpenAI key, transcribe a 2-second clip end-to-end → text
      comes back, contains the spoken words.
- [ ] Same code path against `https://api.lemonfox.ai/v1` with a Lemonfox
      key and `whisper-1` model → works.
