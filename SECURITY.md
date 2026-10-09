# Security

Helpers reads text from other apps, watches the mouse, can paste into other windows, and can hold an API key. We take that seriously.

## Reporting a problem

Please report security problems **privately**, not in a public issue.

Use GitHub's private reporting: go to the **Security** tab of this repository and choose **Report a vulnerability**. Only the maintainers will see it.

Please include:

- What the problem is, and what someone could do with it.
- How to make it happen, step by step.
- The version of Helpers and of Windows.

We'll reply within a week, keep you told how the fix is going, and credit you when it's released, unless you'd rather not be named.

## What counts

Anything that could:

- send the user's text, or their API key, anywhere they didn't choose;
- read text from a password field, an excluded app, or without the user pressing Read, Edit or the shortcut;
- let a downloaded file be swapped without the checksum catching it;
- record what the user types;
- run code that didn't come from this project's own releases.

## How the app is meant to behave

The design is described in [docs/PRIVACY.md](docs/PRIVACY.md). If the app does something that page says it doesn't, that's a bug worth reporting.

## Versions

Only the latest release gets security fixes.
