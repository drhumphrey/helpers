# Contributing to Helpers

Thank you for helping. This project is for anyone who finds reading or writing hard work, so the most useful thing you can tell us is often "this bit was hard".

## Ways to help

- **Try it and tell us what's hard.** Open an issue. Plain words are perfect. Typos are fine.
- **Report a bug.** Use the bug form. Say what you did, what happened, and which app you were reading from.
- **Suggest an idea.** Use the idea form. Say what problem it solves for you.
- **Write code.** Pick an issue, or open one first for anything big, so we can agree the shape before you start.

**Please never paste private text into an issue.** If a bug only happens with certain text, make up a similar example.

## The rules every change keeps

These come from the project's principles. A change that breaks one won't be merged.

1. **Free for everyone.** No paid tiers, no paid extras, no selling AI credits.
2. **A tool, not a treatment.** Describe what it does: reads aloud, checks spelling, gives notes. Never claim it diagnoses, treats or helps with a medical condition.
3. **Offline first.** Anything that can run on the PC does. Cloud features are optional, off by default, labelled with the cloud mark, and use the person's own key.
4. **No telemetry.** No analytics, no crash reporting, no usage pings. Nothing that phones home on its own, except the update check, which is opt-in.
5. **Never log the user's text.** Not in logs, not in error messages, not in crash dumps.
6. **Never store secrets in files.** Keys go in Windows Credential Manager.
7. **Every download is checked** against a SHA-256 built into the app.

## Writing for this app

People reach for this app when reading and writing feel like hard work. Every word on screen should be easy to read.

- UK English: colour, organise, centre.
- Short sentences. One idea each.
- Plain words. "Use" not "utilise". "Start" not "initialise".
- Say what happened and what to do next. "Couldn't reach Claude. Check your internet and try again", not "Request failed".
- No jargon on screen. If a technical word is unavoidable, say what it means.
- Buttons say what they do: "Send to chat", not "OK".

The same goes for issues, pull requests and docs. [docs/DESIGN.md](docs/DESIGN.md) has the visual rules.

## Building and testing

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) on Windows 10 or 11.

```
dotnet build Helpers.slnx
dotnet test Helpers.slnx
dotnet run --project src/Helpers.App
```

- Everything in `Helpers.Core` is unit-tested. New Core code comes with tests.
- Windows-only code lives in `Helpers.Windows`, behind an interface in Core, so a Mac version can share the rest.
- New dependencies need agreeing first, in an issue. Say what it's for, its licence, and whether it uses the network. It must be compatible with the GPL v3.
- [docs/BRIEF.md](docs/BRIEF.md) holds the decisions already made. Its "Decisions already made" table is binding. If you want to change one, open an issue first.

[docs/DEVELOPING.md](docs/DEVELOPING.md) has the full set-up, where everything lives, how to release, and the traps already found. Handy switches for checking your work without clicking through the app (the full list is there too):

| Switch | What it does |
|---|---|
| `--read-file <path>` | Reads a file aloud |
| `--expanded` | Opens the player's reading view |
| `--settings [page]` | Opens Settings, optionally on a page such as `AI` |
| `--compose` | Opens Compose |
| `--compose-check`, `--compose-tidy` | Opens Compose and runs Check my thinking or Tidy on the draft |
| `--lookup <word>` | Opens the word tools |
| `--pill` | Shows the Read button at the mouse |
| `--first-run` | Shows the welcome screen |

## Pull requests

- Keep each one to one change.
- Say what it does and why, in plain words.
- Say how you tested it, and on what.
- Add an entry to [docs/MILESTONES.md](docs/MILESTONES.md) for anything a user would notice.
- If you add a component, add it to `src/Helpers.Core/ThirdPartyNotices.cs` and [docs/THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md).

By contributing, you agree your work is released under the GNU GPL v3 or later, the same as the rest of the project.

## Behaviour

Be kind, be patient, assume good intent. Many people here find reading and writing hard work. Never mock spelling or grammar. See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
