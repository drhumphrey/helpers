# Status

Where the project stands, what's been checked, what hasn't, and what comes next. Written 10 October 2026, when work moved from the first PC to a personal one. Update it as things change; the detail of how each part was built is in [MILESTONES.md](MILESTONES.md).

## In one line

Version 0.1.0 is released on GitHub (10 October 2026) and works on the PC it was made on: reading aloud, the Read button, Compose, the AI helpers on the PC and on Claude, the word tools, settings, the installer and an opt-in update check. Some parts haven't been tried by a user yet.

## Built

| Milestone | What | State |
|---|---|---|
| 0 to 2 | Solution, voice spike, text pipeline | Done |
| 3 | Player, reading view, tray, toasts, vibes | Done, used daily |
| 4 | Selection capture and Ctrl+Alt+Space | Done, checked in Teams, VS Code, Chrome and Outlook |
| 5 | Read button and styled quick menu | Done |
| 6 | Tabbed Settings, pronunciations, readable text, Start with Windows | Done |
| 7 | Packaging: installer, release workflow, welcome screen | Done. v0.1.0 released; the released installer was downloaded, checked against its published checksum, installed silently over an existing copy, registered with Windows, and started |
| 8 | Compose: spelling, Read back, Send to chat, drafts, Edit and Put it back, Paste on the Read button | Done |
| 9 | AI helpers: Check my thinking, Tidy, Make a request; on this PC (Qwen3 4B) and Claude | Done for those three actions |
| 10 | Claude provider, key storage, cost shown, first-use confirmation | Built early, as part of 9 |
| Extras | Word tools, Privacy and licences page, opt-in update check, code scanning, Dependabot | Done |

261 Core tests and 3 Windows tests pass. Every push builds and is scanned on GitHub.

## Tried by the maintainer

- Voices, play, pause, stop, replay, speed, volume, output device.
- The shortcut in Teams, VS Code, Chrome and Outlook.
- Watch clipboard's offer to read.
- Messages on the main monitor with three monitors at different scaling.
- Compose's look, spelling underlines and the right-click menu, after the fixes.
- The welcome screen and the Settings pages, after the fixes for text running off the edge.

## Not tried yet

- **Send to chat** and **Put it back** into a real chat box, and that the clipboard is unchanged afterwards.
- **Edit** and **Paste** on the Read button.
- **Use this window**, and Ctrl+Alt+C picking up the right window.
- **Check my thinking** and **Tidy** on real writing, in daily use.
- **Claude** in the cloud. No key has been entered yet.
- **Word tools** in daily use.
- **The update check** end to end. It needs a newer release than the one installed to find anything.
- **Start with Windows** after a real sign-in, from the installed location.
- From the acceptance list: Word, PDFs in a browser and Acrobat, Notepad, Explorer renaming, Windows Terminal, the Claude desktop app.
- A second PC.

## Known issues and limits

- The local AI takes 10 to 20 seconds per action and uses about 4.7 GB of memory with the voice while loaded. It unloads after 10 idle minutes.
- The local model sometimes mis-quotes a word in its notes, and once changed "Ok" to "Okay" despite being told to keep informal words.
- Output device names are cut at 31 characters.
- Syllables come from hyphenation patterns, so a few split oddly ("in·form·a·tion").
- The app isn't code-signed, so Windows SmartScreen warns on first run.
- The local AI is refused on processors without AVX2, though the library ships a fallback build that might work on older ones. Worth testing before relaxing the check.
- FlaUI brings in about 25 MB of Windows Forms. Calling UI Automation directly would remove it.
- Kokoro has no text normaliser. Numbers, dates and money are handled by rules in `NumberSpeech`; each new oddity heard becomes a rule with a test.

## Not built yet

- **Summarise** and **Explain simply**: the instructions are written and editable in Settings, but there are no buttons. The brief puts them in the player and Compose, with a small result card beside the player.
- The brief's three-way **AI result card** overlay for the player.
- A **microphone choice**. Dictation uses Windows' own voice typing (Win+H), which already works in Compose.
- **macOS** (milestone 11).

## Next, in order

1. **Install v0.1.0 on the second PC** from the [Releases page](https://github.com/drhumphrey/helpers/releases), and set up the development copy there ([DEVELOPING.md](DEVELOPING.md)).
2. **Try the update check** once a newer version is released.
3. **On GitHub:** turn on private vulnerability reporting (Settings, Code security), and add topics.
4. **Dependabot:** five pull requests are open. The test-tool ones can merge once green. Run the app on the five-library group before merging it.
5. **Try everything under "Not tried yet"**, fix what turns up, and log it.
6. **Code signing.** SignPath's free open-source plan now that the repository is public, or Microsoft Trusted Signing.
7. **Summarise and Explain simply** buttons, and the player's result card.
8. **A browser extension** for Chrome and Edge, to put Read and Edit on the right-click menu.
9. **A product name.** "Helpers" is a working title.
10. **macOS.**

## Decisions still open

- The product name.
- When to release, and whether to sign before or after the first release.
- The signing route.
