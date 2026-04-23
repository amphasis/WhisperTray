# Manual test checklist — Phase 8 (tray icon + composition root)

After `dotnet build --configuration Release`, the runnable exe is at
`src/WhisperTray.App/bin/Release/net8.0-windows/WhisperTray.App.exe`.

> Prerequisite: have a valid `settings.json` at
> `%APPDATA%\WhisperTray\settings.json`, or accept the first-run "Configure
> API key" balloon and edit the file by hand (the Settings UI ships in
> Phase 9).

Example minimal settings.json (paste your key in plaintext — the app will
re-save it DPAPI-encrypted on the first Settings window save in Phase 9):

```json
{
  "hotkey": "Ctrl+Alt+Space",
  "provider": "openAi",
  "baseUrl": "https://api.openai.com/v1",
  "model": "gpt-4o-transcribe",
  "audioFormat": "oggOpus",
  "injectionMode": "auto"
}
```

## Startup

- [ ] Launch the exe. No window appears.
- [ ] Tray icon shows in the notification area, tooltip says
      "WhisperTray — Idle".
- [ ] If `ApiKey` is missing/empty → an info balloon appears:
      "Configure API key — right-click the tray icon and open Settings…".
- [ ] If `Hotkey` is invalid in `settings.json` → a warning balloon
      reports the parse error.

## Tray menu

- [ ] Right-click tray icon → context menu with "Settings…", separator,
      "Quit".
- [ ] "Settings…" opens a placeholder MessageBox (until Phase 9).
- [ ] "Quit" closes the app cleanly. Process exits. Tray icon disappears.
- [ ] No orphan processes (`tasklist /fi "imagename eq WhisperTray.App.exe"`
      returns empty).

## End-to-end dictation (needs API key + mic)

- [ ] Focus a Notepad window.
- [ ] Press `Ctrl+Alt+Space`. Tooltip changes to "Recording…". No key
      character leaks to Notepad.
- [ ] Speak "hello world".
- [ ] Press `Ctrl+Alt+Space` again. Tooltip briefly shows "Transcribing…"
      then "Inserting…" then back to "Idle".
- [ ] The text "hello world" (or similar) appears in Notepad via paste.
- [ ] Clipboard is restored to its prior contents after ~2 seconds.

## Auto mode downgrade

- [ ] Right-click an elevated cmd → Run as administrator. Focus the
      elevated cmd window.
- [ ] Press hotkey, speak, press hotkey again.
- [ ] Text does NOT appear in cmd. Instead an info balloon reads
      "Text routed to clipboard" / "…runs elevated…". Clipboard contains
      the transcription.

## Hotkey collision / bad settings

- [ ] Set `"hotkey": "Ctrl+Alt+QQQ"` in settings.json, relaunch. Warning
      balloon reads "Invalid hotkey — Unknown key: …". No hotkey registered.
- [ ] Correct the hotkey, relaunch. Hotkey works.

## Network error surfaces

- [ ] Disconnect network, record, stop. Error balloon reads
      "Transcription failed — …".
- [ ] State returns to Idle; hotkey still works after reconnecting.
