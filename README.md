# helpers

Free, open-source tools for people with dyslexia who work with AI chat assistants. Windows first, with macOS planned.

AI assistants are brilliant, but working with them means reading a lot of long text and typing a lot of precise requests. That is hard work if you have dyslexia, are not a natural typer, or find it easier to think than to put thoughts in order on the page. These tools take that strain off, and keep your text on your own PC.

**Status: early design.** There is nothing to install yet. The first app is specified in [docs/BRIEF.md](docs/BRIEF.md), and a working prototype is in [prototype/](prototype/).

## Principles

- **Free for everyone.** No tiers, no paid extras, no selling AI credits. Ever.
- **Offline first.** Voices and text tools run on your PC. Nothing leaves it unless you switch on a clearly labelled cloud feature and give it your own API key.
- **Private.** No telemetry. Your text is never logged.
- **Open source** under the GNU GPL v3. See [LICENSE](LICENSE).

## What is planned

**Read Aloud.** Select text in any app, click the small Read button that appears, and hear it in a natural offline voice. A compact player gives you pause, skip and speed. It understands the Markdown that AI chats produce, so code blocks and formatting symbols are not read out letter by letter.

**Compose.** A scratchpad for writing to the AI. Spelling as you type, a button to hear your draft read back, and one click to send it to the chat without pressing Enter for you. Open it with Ctrl+Alt+C from the window you want to send to. Coming next: a Tidy button that fixes grammar and puts your thoughts in order without changing what you mean.

## Try the prototype today

The prototype is an AutoHotkey script that uses the voices already on Windows.

1. Install [AutoHotkey v2](https://www.autohotkey.com/).
2. Double-click `prototype/ReadAloud.ahk`.
3. Select some text anywhere and press **Ctrl+Alt+Space**. Press it again to stop.
4. Right-click the tray icon to change voice or speed.

## Repository layout

| Folder | Contents |
|---|---|
| `docs/` | The build brief and design notes |
| `prototype/` | The AutoHotkey v2 proof of concept |
| `src/` | The .NET application |
| `tests/` | Unit tests |
| `tools/` | Spikes and developer tools. `EngineSpike` is the milestone 1 voice check; `ReadingDump` prints what the reading pipeline would say for a file. |

## Installing

Grab the latest release from the Releases page: either the installer, `Helpers-x.y.z-setup.exe`, which installs for your user only and needs no admin rights, or the zip, which runs from any folder.

The build is not code-signed yet, so Windows SmartScreen will warn on first run. Choose "More info" then "Run anyway". The first run downloads the voice, about 350 MB, once, into `%LOCALAPPDATA%\Helpers\models`. Settings live in `%APPDATA%\Helpers\settings.json`.

Then: select text in any app and press **Ctrl+Alt+Space**, or click the small **Read** button that appears when you select with the mouse. The tray icon has the rest.

## Building

Install the .NET 10 SDK, then from the repo root run `dotnet build Helpers.slnx` and `dotnet test Helpers.slnx`. To run the app: `dotnet run --project src/Helpers.App`. To make a release: push a tag such as `v0.1.0` and the Release workflow publishes, zips, builds the installer with Inno Setup and attaches both to a GitHub release.

To hear the voices, run `dotnet run --project tools/EngineSpike`. The first run downloads the Kokoro voice model (about 350 MB) into your local app data folder, then reads a paragraph aloud with two British voices and prints timing and memory figures. Add `--no-play` to skip playback, or `--runs bf_lily:1.2` to try other voices and speeds.

## Contributing

Issues and pull requests are welcome. Please keep to the principles above: free, offline first, private. UK English in UI text and documentation.
