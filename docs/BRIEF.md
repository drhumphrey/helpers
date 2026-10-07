# Read Aloud for Windows: build brief

## What we're building

A small Windows 10/11 tray app that reads selected text aloud from any app: Word, Outlook (classic and new), Teams, Chrome, Edge, PDFs, Notepad and so on.

- **No hotkey needed.** When you select text with the mouse, a small "Read" button appears next to the cursor. Click it and it reads. Nothing is copied or read until you click.
- **A compact player** handles pause, skip and speed.
- **Voices are open-source neural voices running entirely on the PC.** No account, no per-use cost, nothing leaves the machine.
- **AI translation is optional** and off by default. When switched on, only the text you choose to translate is sent off the machine.

`prototype/ReadAloud.ahk` (AutoHotkey v2) is a working prototype. Use it as the reference for clipboard capture, clipboard restore and text clean-up behaviour. Don't port AutoHotkey itself.

## Working agreement

- Build milestone by milestone (see below). At the end of each one, stop and show Dave what works, with a short note of anything that didn't go to plan.
- Ask before changing any decision in the next section, and before adding dependencies beyond those listed.
- Unit-test everything in `ReadAloud.Core`.
- Never commit API keys or put user text in logs.
- Use UK English in all UI text.

## Decisions already made

| Area | Decision |
|---|---|
| Runtime | C# on .NET 10 (LTS). Windows only, x64 first; add ARM64 later if it's cheap. |
| UI | WPF with the Fluent theme that ships with .NET 9+ (switch to the WPF-UI library if that proves limiting). Follow the Windows light/dark setting. |
| Speech | sherpa-onnx (NuGet `org.k2fsa.sherpa.onnx`) running Kokoro, whose weights are Apache-2.0, fully offline. |
| Default voice | A British Kokoro voice. v1.0 includes bf_emma, bf_isabella, bf_alice, bf_lily, bm_george, bm_lewis, bm_daniel and bm_fable. Check the sherpa-onnx docs for the current Kokoro model package and its speaker-ID mapping. Prefer a quantised (int8) build if quality is close. |
| Audio | NAudio. |
| Reading the selection | UI Automation first (UIA3 via COM, e.g. FlaUI.UIA3 or direct interop). Clipboard as fallback. |
| Privacy | Offline by default. Cloud features are opt-in, per feature, and clearly labelled. |

Prerequisites on the dev machine: the .NET 10 SDK (VS Code with the C# Dev Kit is enough). Inno Setup is needed for the installer milestone.

## User experience

### Read button (the main trigger)

- **When it appears:** after a mouse text selection (drag-select, double-click or triple-click). It's a small pill near the cursor showing "Read" with a speaker icon, plus "Translate" when translation is enabled.
- **Focus:** it must never steal focus. Use a non-activating window.
- **When it must not appear:**
  - window drags, file drags and scrollbar drags
  - clicks on our own windows
  - apps on the excluded list
- **When it hides:** after about 3 seconds, or on any click elsewhere, key press or scroll.
- **Clicking "Read"** captures the selection and starts reading.
- **Keyboard selections** (Shift+arrows) don't trigger the pill. The optional hotkey covers those.

### Player

- **Window:** a small floating bar. It stays on top, is draggable, remembers its position and can be minimised to the tray.
- **Controls:**
  - play/pause and stop
  - back/forward one sentence
  - speed slider: 0.5x to 2.0x in 0.1 steps, default 1.0x
  - voice picker
- **Text:** shows the current sentence, highlighted as it's read.
- **When it shows:** it appears when reading starts. It hides a few seconds after reading ends; this is a setting.
- **Focus:** clicking its buttons must not take focus from the app being read.

### Tray icon

The menu has:
- Read clipboard
- Pause/Resume
- Stop
- Show player
- Settings
- Pause the Read button for 1 hour
- Start with Windows
- Exit

### Optional hotkey

Off by default and configurable; suggest Ctrl+Alt+Space. It reads the current selection, or stops reading if already reading.

### Settings window

- voice, with a preview button
- default speed
- Read button on/off and its delay
- excluded apps (by process name)
- hotkey on/off and the key itself
- pronunciation dictionary editor
- Start with Windows
- the cloud features section (see AI integration)

## Architecture

One solution, suggested projects:

| Project | Responsibility |
|---|---|
| `ReadAloud.App` | WPF: tray, pill, player and settings windows, composition root. |
| `ReadAloud.Core` | Text clean-up, sentence splitter, pronunciation dictionary, settings model, cost estimates. Interfaces: `ISpeechEngine`, `ITranslator`, `ISelectionSource`. No Windows dependencies. |
| `ReadAloud.Windows` | Mouse hook, UIA capture, clipboard capture/restore, non-activating window helpers, start-with-Windows registration. |
| `ReadAloud.Speech` | sherpa-onnx Kokoro engine, NAudio playback queue. |
| `ReadAloud.Tests` | Unit tests for Core. |

Threading:

- **Mouse hook:** the low-level mouse hook (`WH_MOUSE_LL`) runs on its own thread with a message loop. The callback only queues the event and returns immediately.
- **Speech worker:** one worker owns the sherpa-onnx `OfflineTts` instance. Create it once and reuse it; never call it from more than one thread.
- **Look-ahead:** synthesis runs one or two sentences ahead of playback, so audio starts fast and never gaps between sentences.

## Component details

### Selection capture

1. **UIA first.** Get the focused element. If it supports TextPattern, read `GetSelection()`.
   - Never read password fields (`IsPassword`).
   - Time-box UIA calls to about 150 ms. Chromium builds its accessibility tree lazily, so the first query in a Chrome, Edge, Teams or new Outlook window can be slow.
2. **Clipboard fallback.**
   - Save the clipboard (every format you can), then clear it.
   - Send Ctrl+C, or Ctrl+Insert in Windows Terminal and console windows, where Ctrl+C with nothing selected means "cancel".
   - Wait up to 1 s for text, then restore the original clipboard.
   - The user's clipboard must never end up changed.
3. **Elevated windows.** When we're not elevated, don't attempt capture from admin windows (Windows blocks it anyway). Show a short hint instead.

### Text clean-up (Core, unit-tested)

Port from `ReadAloud.ahk`:
- URLs become "link"
- bullet characters are stripped
- blank lines are collapsed
- a line break with no punctuation before it becomes a pause (". ")

Add:
- **Email addresses** read as "email address" (a setting).
- **Pronunciation dictionary.** User-editable, whole-word matching, optional case-sensitivity. Ship a small starter list in the style of `EVAR → ee-var`, and store it in settings.
- **Sentence splitter.** It must cope with abbreviations ("Dr.", "et al.", "e.g.", "Fig. 2", decimals). Cap chunks at about 400 characters for the engine.

### Speech engine

- **Loading:** load the model in the background at app start. If a read is requested before it's ready, show "Loading voice…" in the player.
- **Speed:** use the engine's own speed parameter, not audio time-stretching.
- **Preview:** the voice preview speaks a fixed sample sentence.
- **Model files:** they live in `%LOCALAPPDATA%\ReadAloud\models\`. Either the first run downloads them (with progress and a SHA-256 check) or the installer ships them; decide at the packaging milestone. Make the path configurable for offline installs.
- **Memory:** add an "Unload voice when idle for N minutes" setting in case memory use is high.

### Playback

- **Queue:** an NAudio streaming queue with pause/resume, skip back/forward a sentence, and stop.
- **Output device:** use the Windows default output device, and cope with device changes (headphones plugged in mid-read) without crashing.

## AI integration (optional, off by default)

The principle: keep the voice local because it's free, private and fast. Use AI only where local models are weak, which is translation.

### Translation

- **Provider:** an `ITranslator` interface. The first implementation uses the Claude Messages API with the cheapest current model, which is `claude-haiku-5-5` at the time of writing. Make the model ID a setting, and check Anthropic's models page when building. Use Anthropic's official .NET SDK if one exists; otherwise use plain `HttpClient`.
- **Token discipline:**
  - Send only the selected text, plus a one-line system prompt: "Translate into {language}. Output only the translation. Keep medical terminology and abbreviations accurate."
  - Send no history.
  - Set `max_tokens` to about 1.5x the input tokens, with a minimum of 256.
  - Cap input length at 4,000 characters by default (configurable), and warn above it.
  - Cache results by a hash of (text, target language, model) for the session.
- **Cost tracking:** record the `usage` token counts the API returns and show a running monthly cost estimate in Settings. At current Haiku list prices an email-length translation costs a tiny fraction of a penny.
- **Reading the result:** read it with a Kokoro voice in the target language if the sherpa-onnx build supports that language; Kokoro covers several beyond English, so check which. Otherwise show the translation in the player and say no local voice is available for that language.
- **API key:** store it with Windows Credential Manager or DPAPI, never in plain-text settings.
- **Transparency:** make it obvious when text is about to leave the machine. Show a first-use confirmation, and put a small cloud icon on the Translate button.
- **Other providers:** leave room for DeepL or Azure Translator behind the same interface. Offline translation models exist but are weaker, especially on medical terms; they're not in v1.

### Cloud "premium" voice (not v1)

A possible second `ISpeechEngine` implementation for a cloud voice (Azure neural, OpenAI and so on), built only if Dave asks for it. These are metered per character, roughly a penny or two per minute of audio, and the text leaves the machine.

## Privacy and security

- No telemetry. No selected text, translations or audio in logs or crash reports.
- Settings live in `%APPDATA%\ReadAloud\settings.json`; secrets live in Credential Manager.
- Never read password fields.
- Skip excluded apps. The default list is common password managers (1Password, Bitwarden, KeePass) and `mstsc.exe`.
- Cloud calls happen only when the user clicks Translate, over HTTPS, with no extra metadata.

## Performance targets

Measure and report these at milestone 1, then adjust:

- **Idle CPU:** about 0%.
- **Memory:** report it with the model loaded. Aim for under 400 MB; use the int8 model if that helps.
- **Time to first audio:** under 1 s from clicking Read, on a typical work laptop, for a normal sentence.
- **Pill delay:** the pill appears within 200 ms of mouse-up.

## Packaging

- **Build:** `dotnet publish`, self-contained, win-x64.
- **Native libraries:** make sure sherpa-onnx's native DLLs load correctly in a single-file publish (check `IncludeNativeLibrariesForSelfExtract`), or ship them next to the exe.
- **Outputs:** a zip, and an Inno Setup installer that installs per user and needs no admin rights.
- **Start with Windows:** use the HKCU Run key.
- **Signing:** optional. Note in the README that unsigned builds trigger a SmartScreen prompt on first run.

## Milestones

Demo each one before starting the next.

1. **Engine spike.** A console app that reads a paragraph aloud with Kokoro via sherpa-onnx, using two British voices at 1.0x and 1.5x. Report model load time, time to first audio and memory use. This milestone is also the voice-quality check: if the voices aren't good enough, stop here.
2. **Player and tray.** Read clipboard text with the full player controls and sentence highlighting.
3. **Selection capture.** UIA plus the clipboard fallback, tested against the checklist below.
4. **Read button.** Mouse hook, show/hide rules, non-activating window, multiple monitors and DPI.
5. **Settings.** Pronunciation dictionary, Start with Windows, excluded apps, optional hotkey.
6. **Packaging.** Installer and model download.
7. **Optional: Translate via Claude,** with cost tracking.

## Acceptance checklist

For each app: select text with the mouse, click Read, hear it, and confirm the clipboard is unchanged afterwards.

- **Apps:**
  - Word
  - Outlook (classic and new)
  - Teams (new)
  - Chrome and Edge
  - a PDF in Edge and in Acrobat Reader
  - Notepad
  - File Explorer: renaming a file must not trigger the pill
  - Windows Terminal: no Ctrl+C is sent when nothing's selected
- **No pill:** when dragging a window by its title bar, dragging files or resizing windows.
- **Double-click and dismiss:** double-clicking a word shows the pill, and clicking elsewhere removes it.
- **Clipboard formats:** if the clipboard holds an image or rich text before a read, it's still intact afterwards.
- **Multiple monitors:** with two monitors at different scaling, the pill appears next to the cursor on both.
- **Interruptions:** the laptop sleeping or headphones being plugged in mid-read causes no crash.
- **Password fields:** selecting text in one reads nothing.

## Gotchas

- **Non-activating WPF windows:**
  - set `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST` in `SourceInitialized`
  - set `ShowActivated = false`
  - return `MA_NOACTIVATE` for `WM_MOUSEACTIVATE`
- **Low-level hooks:** Windows silently removes them if the callback is slow. Never do UIA or clipboard work inside the callback.
- **UIPI:** a non-elevated app can't read from or send keys to elevated windows.
- **Clipboard restore:** some formats are delayed-rendered or app-private and can't be restored. Restore what you can (text, Unicode text, RTF, HTML, images, file lists) and test with Office.
- **DPI:** declare per-monitor v2 DPI awareness in the app manifest.

## Out of scope for v1 (ideas for later)

- reading text in images via the Windows OCR API
- "Save as MP3"
- per-app voice and speed
- an ARM64 build
- a cloud premium voice
- offline translation
