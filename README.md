# WhisperTray

Windows tray-resident dictation app. Hit a hotkey → record audio → transcribe via an OpenAI-compatible Whisper API → type the result into the focused window.

## Status

Early scaffolding. See the phased plan below.

## Build

```
dotnet build
dotnet test
```

Requires .NET 8 SDK on Windows.

## Layout

- `src/WhisperTray.Core` — platform-agnostic business logic (target `net8.0`).
- `src/WhisperTray.App` — WPF tray host with Win32 adapters (target `net8.0-windows`).
- `tests/WhisperTray.Core.Tests` — xUnit unit tests for Core.

## Roadmap

Implementation proceeds phase-by-phase; each phase is a single commit with a green build and green tests.

0. Solution scaffold + CI
1. Settings model + JSON store
2. Transcription client (OpenAI-compatible)
3. Audio encoding pipeline (Opus/OGG)
4. Text injection strategy
5. Hotkey parsing
6. Orchestrator state machine
7. Real OS adapters (hotkey, audio, injection) in App project
8. Tray icon and app host
9. Settings window
10. Autostart
11. Packaging and docs
