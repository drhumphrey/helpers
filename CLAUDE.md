# Working on helpers

Start with `docs/STATUS.md` for where things stand and what comes next. Read `docs/BRIEF.md` before writing any code. Its "Decisions already made" table is binding. Ask before changing a decision there or adding a dependency it does not list. `docs/DEVELOPING.md` has the set-up, the developer switches, the release steps and the traps already found; `docs/MILESTONES.md` is the build log.

## How we work

- Build milestone by milestone, in the order the brief gives. Demo each one before starting the next, with a short note of anything that did not go to plan.
- Log each piece of work in `docs/MILESTONES.md`, and keep `docs/STATUS.md` current.
- Unit-test everything in `Helpers.Core`.
- UK English in all UI text, comments and docs.
- Never commit API keys. Never put user text in logs.
- Describe the app by what it does: it helps people read, write and think things through. No condition named in public text, and never anything that suggests it is a medical aid.

## On the maintainer's PC

- Never send synthetic mouse clicks or key presses to the desktop. To test the app's menus, post messages to the app's own windows only (`tools/screenshots/post-right-click.ps1`).
- Never screenshot anything but the app's own windows. Use `tools/screenshots/shot.ps1`, which finds a window by its exact title and refuses anything else. Never commit a screenshot that shows anything private.
- Before planting test data, back up `%APPDATA%\Helpers\settings.json`, mute the voice, and put the settings back afterwards. `tools/screenshots/capture.ps1` does all three.
- Stop the running app before building or publishing over it.

## Principles that must hold

- Free for everyone. No tiers, no paid extras, no selling AI credits.
- Offline first. Cloud AI is optional, per feature, off by default, and bring-your-own-key.
- No telemetry. The only call the app makes by itself is the update check, and only when the user has switched it on.

## Working with Dave

Lead with the answer. Short sentences. Bullets over paragraphs. Keep code out of prose. Expect typos in his messages and read for intent.
