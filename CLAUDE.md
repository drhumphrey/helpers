# Working on helpers

Read `docs/BRIEF.md` before writing any code. Its "Decisions already made" table is binding. Ask before changing a decision there or adding a dependency it does not list.

## How we work

- Build milestone by milestone, in the order the brief gives. Demo each one before starting the next, with a short note of anything that did not go to plan.
- Unit-test everything in `Helpers.Core`.
- UK English in all UI text, comments and docs.
- Never commit API keys. Never put user text in logs.

## Principles that must hold

- Free for everyone. No tiers, no paid extras, no selling AI credits.
- Offline first. Cloud AI is optional, per feature, off by default, and bring-your-own-key.
- No telemetry.

## Working with Dave

Lead with the answer. Short sentences. Bullets over paragraphs. Keep code out of prose. Expect typos in his messages and read for intent.
