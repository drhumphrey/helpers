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
