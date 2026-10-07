# helpers

Free, open-source Windows tools for people with dyslexia who work with AI chat assistants.

AI assistants are brilliant, but working with them means reading a lot of long text and typing a lot of precise requests. That is hard work if you have dyslexia, are not a natural typer, or find it easier to think than to put thoughts in order on the page. These tools take that strain off, and keep your text on your own PC.

**Status: early design.** There is nothing to install yet. The first app is specified in [docs/BRIEF.md](docs/BRIEF.md), and a working prototype is in [prototype/](prototype/).

## Principles

- **Free for everyone.** No tiers, no paid extras, no selling AI credits. Ever.
- **Offline first.** Voices and text tools run on your PC. Nothing leaves it unless you switch on a clearly labelled cloud feature and give it your own API key.
- **Private.** No telemetry. Your text is never logged.
- **Open source** under the GNU GPL v3. See [LICENSE](LICENSE).

## What is planned

**Read Aloud.** Select text in any app, click the small Read button that appears, and hear it in a natural offline voice. A compact player gives you pause, skip and speed. It understands the Markdown that AI chats produce, so code blocks and formatting symbols are not read out letter by letter.

**Compose.** A scratchpad for writing to the AI. Spelling as you type, a button to hear your draft read back, and a Tidy button that fixes grammar and puts your thoughts in order without changing what you mean. Then one click sends it to the chat.

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
| `src/` | The .NET application (coming) |
| `tests/` | Unit tests (coming) |

## Building

The .NET app will need the .NET 10 SDK. Build instructions will appear here once the first milestone lands.

## Contributing

Issues and pull requests are welcome. Please keep to the principles above: free, offline first, private. UK English in UI text and documentation.
