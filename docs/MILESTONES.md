# Milestone log

One entry per milestone: what was built, what was measured, what didn't go to plan. Newest at the bottom.

## Milestone 0: scaffolding (8 October 2026)

**Built:** `Helpers.slnx` with Core, Speech, Ai, Windows and App projects, an xUnit test project, shared build settings in `Directory.Build.props`, and a GitHub Actions workflow that builds and tests on every push.

**Not to plan:**

- The Avalonia template installed Avalonia 12, not 11. Same licence, brief updated.
- The .NET 10 SDK writes solutions as `.slnx`, so the file is `Helpers.slnx`.
- winget hung on this PC while refreshing its index, so the SDK was installed with Microsoft's direct installer.
- The checkout lives in Dropbox. `.git` and every `bin` and `obj` folder are marked Dropbox-ignored instead of moving the folder.

## Milestone 1: engine spike (8 October 2026)

**Built:** `tools/EngineSpike`, a console program that downloads the Kokoro model package once, loads it in sherpa-onnx, speaks a 325-character paragraph with two British voices, streams audio to the speakers as the engine produces it, saves WAV files, and prints the numbers below.

**Model:** `kokoro-multi-lang-v1_0` from the sherpa-onnx releases, 350 MB download, full precision. It is the only sherpa-onnx package with the British voices. No int8 build of it exists; the only int8 Kokoro package is v1.1, which is Chinese-focused with three English voices.

**Machine:** Dave's work PC, Windows 11, CPU only, 4 engine threads.

| Measure | Result | Brief target |
|---|---|---|
| Model load | 850 to 900 ms | not set |
| Time to first audio, first ever run | 855 ms | under 1 s |
| Time to first audio, warm | 130 to 230 ms | under 1 s |
| Synthesis speed | 6 to 8 times faster than real time (RTF 0.12 to 0.16) | not set |
| Memory after load | about 450 MB working set | under 400 MB |
| Memory after three paragraphs | about 700 MB working set, 860 MB private | under 400 MB |

**Voice quality:** Dave's call. The WAV files are in `out/engine-spike/` and the spike can be re-run with any of the eight British voices: `dotnet run --project tools/EngineSpike -- --runs bf_lily:1.0,bm_daniel:1.0`.

**Not to plan:**

- **Memory is over target and grows with use.** The engine's working set rises from 450 MB to 700 MB over three paragraphs, with playback switched off, so the growth is in onnxruntime, not the spike. Things to try at milestone 3: synthesise one sentence at a time rather than a whole paragraph (the arena grows with the longest input), look for an onnxruntime arena setting sherpa-onnx exposes, quantise the model to int8 ourselves, and rely on the planned "unload when idle" setting.
- **No int8 British model** exists ready-made, so the brief's "prefer int8" can't be followed yet.
- **NAudio 3 renamed things.** `WaveOutEvent` is now `WaveOut`, and the buffer size is set in the constructor. Worth knowing before milestone 3.
- **The spike targets `net10.0-windows`** because NAudio's playback classes only ship for Windows frameworks. The real `IAudioOutput` for Windows will need the same.

## Milestone 2: text pipeline (8 October 2026)

**Built:** the reading pipeline in `Helpers.Core.Text`, with 80-plus unit tests. Captured text goes in; a list of segments comes out, each with the text to show, the text to speak, and a pause to leave afterwards.

- `MarkdownDetector` decides whether text is Markdown (AI chat) or plain (email, documents).
- `PlainTextSegmenter` ports the prototype rules: bullets stripped, blank lines dropped, one segment per line.
- `MarkdownSegmenter` uses Markdig: headings and bold-only lines pause longer, bold and inline code keep their words, code blocks become "code block, 12 lines", lists go one item at a time with numbers spoken, tables get a caption then one row per segment as "column: value; column: value".
- `SentenceSplitter` splits on full stops, question and exclamation marks but not after Dr., e.g., et al., Fig. 2, initials, list numbers, decimals, version numbers or file names, and chunks anything over 400 characters at a clause boundary.
- `SpokenTextRules` turns web addresses into "link", email addresses into "email address", and file paths into "file name.ext", all switchable.
- `PronunciationDictionary` does whole-word replacement with optional case sensitivity and ships a starter list.
- `ReadingPipeline` strings them together.

**Test data:** two real Claude Code replies from this project, saved under `tests/Helpers.Tests/Fixtures`. The acceptance checks from the brief run against them: no stars, hashes, backticks or pipes are ever spoken, code is skipped with a count, tables read row by row, nothing handed to the engine is over the limit.

**Tool:** `tools/ReadingDump` prints the pipeline's output for any file, so a reply can be checked by eye before it is heard.

**Not to plan:**

- Markdig is at version 1.4, not the 0.x series the docs online mostly describe. The API was the same.
- Two things only showed up when reading real output rather than unit tests: a quoted abbreviation such as "e.g." wrongly ended a sentence, and table rows joined with commas were hard to follow. Both fixed and now covered by tests.
- Bold-only lines were added as headings. The brief didn't ask for it, but every Claude Code reply uses them that way.

## Milestone 3: player and tray (started 8 October 2026)

**Step 1, the overlay spike: passed.** An Avalonia 12 window can be clicked without taking focus. `OverlayWindow` in the app project is borderless, topmost, off the taskbar, translucent with acrylic blur, and refuses activation two ways: the Win32 no-activate and tool-window styles are added once the window opens, and a message hook answers `WM_MOUSEACTIVATE` with `MA_NOACTIVATE`. The test: Notepad in front, two simulated clicks on a button inside the overlay, Notepad still in front, the button's counter at 2. Every overlay surface (pill, player, toasts, AI card) will inherit from this class.

**Also in step 1:** Lexend is now the app font, bundled under `src/Helpers.App/Assets/Fonts` with its licence, replacing the template's Inter package. The app has no main window: it starts in the tray with a menu, and `ShutdownMode` is explicit.

**Not to plan so far:**

- Avalonia 12 renamed `SystemDecorations` to `WindowDecorations`. Expect more renames like it; the online docs mostly describe 11.
- `Helpers.App` and `Helpers.Windows` now target `net10.0-windows` rather than plain `net10.0`, because NAudio's playback classes only ship for Windows frameworks and a plain project can't reference a Windows one. The Mac build will need its own target later; Core, Speech and Ai stay platform-neutral.

**Step 2, the player (8 October 2026, evening).** Reading works end to end: text goes through the pipeline, `ReadingSession` synthesises one sentence ahead and plays through NAudio, the player shows the current sentence and expands into the reading view with click-to-jump, toasts stack above the tray, and the tray menu has Read clipboard, Watch clipboard, Pause, Stop, Show player, Settings, Calm look, Memory in use and Exit. Settings persist in `%APPDATA%\Helpers\settings.json`. Neon and Calm are resource dictionaries swapped at runtime.

**Dave's first test:** every voice, play, stop, replay and speed all fine. He asked for volume, an output device choice, a settings cog, and a settings area, and reported that the player could grow off the edge of his monitor and that toasts could straddle two monitors.

**Step 3, from that feedback.** A first Settings window, pulled forward from milestone 6: voice with preview, speed, volume, output device, vibe, light or dark, hide delay, watch clipboard. A cog on the player and a Settings item in the tray open it. Overlays now open on the monitor the mouse is on and nudge themselves back inside that screen whenever their size changes; the reading view's height scales to the screen. A microphone choice waits for dictation.

**Not to plan in steps 2 and 3:**

- Acrylic blur applies to the whole window rectangle on Windows, so the transparent shadow margin became a frosted box. Overlays now use plain transparency with a near-solid card instead; the glow fades properly, the blur-through is gone.
- Play on a finished reading did nothing. Now it restarts; clicking a sentence after the end starts from there.
- NAudio 3 again: `DesiredLatency` is gone; use `BufferMilliseconds` and `NumberOfBuffers`. Avalonia 12: `NativeMenuItemToggleType` is now `MenuItemToggleType`.
- A build interrupted by a file lock left a truncated DLL in `obj`, which then failed every later build with "Image is too small" from the app-host step. `dotnet clean` on the project fixes it.
- Output devices come from the old WinMM API, whose names are cut at 31 characters. WASAPI gives full names and device-change events; worth switching when device hot-plugging is handled.

**Still to do in milestone 3:** the drifting flecks, the speed slider in the gradient rather than the default blue, the memory figure in the log once measured in the real app, and an app icon once the product has a name.

**Step 4, from Dave's second round (8 October 2026, evening).** Overlays now clamp to the screen that holds their top-left corner, so growing across a monitor edge pulls them back instead of pushing them over. The reading view is resizable by a grip in its corner and remembers its size. A Size setting scales every overlay from 60% to 125%, default 90%. The Custom vibe takes two or three hex colours, with six named palettes to start from. The border gradient and the sentence highlight drift while the voice is speaking, with a switch to stop it. The compact bar no longer cuts sentences off: it renders the sentence word by word, marks the word being spoken, and scrolls so that word's line is visible. Kokoro gives no word timings, so the word is estimated from the clip's length and the words' lengths, with extra weight for punctuation.

**Not done from that round:** the tray menu is a native Windows menu and can't take the vibe styling; a styled quick menu comes with the Read button in milestone 4, along with the hotkey and send-to-reader Dave asked for.

**Step 5, from Dave's third round.** In the reading view the sentence box now sits on its own full-width line under the controls, so narrowing the window can't push it under the icons. The open/close button shows which state it is in. The word being spoken is marked on the current line of the reading view as well as in the bar. Whether the player was left open is remembered. The Custom vibe's colours are now picked with colour wheels (Avalonia's own ColorPicker package, same licence) and apply as you pick; the Apply button is gone. A switch adds the optional third colour.

**Step 6, closing the milestone.** Flecks: glowing dots in the gradient colours plus lime, placed in the band of ground around the card and wandering slowly; a Flecks slider in Settings sets how many, Calm has none, and Windows' "animation effects off" setting freezes them along with the gradient drift. Sliders in every vibe take the vibe's colours. A multi-size app icon built from the speaker mark is embedded in the exe and used for the tray; it is name-agnostic, so the product name can come later.

**Milestone 3 measured:** about 750 MB working set while reading a long reply, the same as the engine spike, so reading sentence by sentence did not shrink the engine's arena. Idle CPU is effectively zero: the only timer that runs while idle is the 40 ms fleck drift, and only while the player is visible.

**Milestone 3 done, 8 October 2026.** Not in the brief but built on Dave's requests along the way: volume, output device, a first Settings window, UI scaling, a resizable reading view, custom gradient colours with colour wheels, gradient motion, word-level highlighting. Deferred to milestone 4 and 5: the hotkey, selection capture, the Read button, and a styled quick menu in place of the native tray menu.

## Milestone 4: selection capture and the hotkey (started 8 October 2026)

**Built:** `SelectionCapture` in `Helpers.Windows`, behind `ISelectionSource` in Core. UI Automation first through FlaUI, time-boxed to 150 ms; then the clipboard: every copyable format is saved, the clipboard is cleared, Ctrl+Insert is sent, the app waits up to 350 ms for text, and only then, and never to a terminal, VS Code or Cursor, does it try Ctrl+C for up to a second. The saved clipboard is always put back. Windows running as administrator are detected and refused with a hint. Password fields are refused.

**The hotkey:** a hidden message-only window on its own thread (`MessageWindow`) registers a global shortcut, default Ctrl+Alt+Space, on by default because Dave asked for it. Pressed while reading, it stops; otherwise it reads the selection. Settings has a key-capture box, an on/off switch, and a message if Windows refuses the combination because another app owns it. Verified from a second process: Windows refuses to register Ctrl+Alt+Space while the app runs, so the app holds it.

**Also in Core:** `HotkeyGesture` parses and formats shortcut text, with tests.

**Dave's test, 8 October 2026, evening:** the shortcut works in Teams, VS Code, Chrome and Outlook. Three things came out of the testing and were fixed the same evening: a teardown race when a new read replaced a running one; the shortcut now reads a new selection even mid-read and stops only when nothing is selected; and Watch clipboard, which read every copy, now only offers a Read button in a fading toast. Also added from the testing: years, money, percentages, clock times and ordinals spoken naturally (`NumberSpeech`), an unload-when-idle setting for the voice's memory, messages pinned to the main monitor by default, and placement that settles twice because Dave's monitors have different scaling. Word, PDFs, Notepad, Explorer and Windows Terminal are still to be ticked off the checklist.

**Milestone 4 done, 8 October 2026.**

## Milestone 5: the Read button and the quick menu (started 8 October 2026)

**Built:** low-level mouse and keyboard hooks on the hotkey's message thread (`InputHooks`); the callbacks only read the event and raise it, and the keyboard hook carries no key codes, only "a key was pressed". A platform-neutral `SelectionGestureDetector` in Core turns the events into drag-select, double-click and triple-click, with tests. `ReadButtonService` applies the brief's rules before showing the pill: not when the press was on a frame (title bar, border, scrollbar, checked with a 50 ms hit-test), not when the release is over a different window (a window or file drag), not over our own windows, not in excluded apps (password managers and remote desktop to start with), and not while paused from the tray. The pill is an overlay capsule with one Read button that uses the same capture as the shortcut; it hides after three seconds or on any click, key or scroll elsewhere. Settings gained a Read button section: on/off, delay, excluded apps.

**The quick menu:** a left-click on the tray icon opens a styled menu in the vibe's colours with the same items as the native menu, plus ticks for the toggles. It closes on a choice, a click anywhere else, a key press, or after ten seconds. The native right-click menu stays.
