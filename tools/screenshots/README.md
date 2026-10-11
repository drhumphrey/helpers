# Screenshot tools

Scripts for taking the README screenshots, and for checking menus without touching the mouse. Windows PowerShell 5.1, no extra modules.

They only ever capture or message the app's own windows, found by exact title and checked to belong to `Unmuddle`. They never screenshot the desktop and never send real clicks or key presses, so they're safe to run while you're working.

| Script | What it does |
|---|---|
| `capture.ps1` | The whole run: backs up your settings, plants a made-up draft and the app's default lists, mutes the voice, opens Compose (with Check my thinking), the player large and small, the word tools, the Read button, the menu, the welcome screen and each Settings page in turn, captures each, puts your settings back, and writes cleaned images to `docs/images` for the README and the how-to guide. Compose shows the app that was in front as "Sending to": check it before committing. |
| `shot.ps1` | Captures one of the app's windows by its exact title, through `PrintWindow`, so it works even with other windows in front. |
| `clean.ps1` | Makes the near-black margin around a capture transparent, so the image sits well on light and dark pages. |
| `post-right-click.ps1` | Posts a right-click to one of the app's own windows at a point, lists any window that opens (such as a context menu), captures it, then closes it. For checking menus. |
| `demo-reply.md` | The made-up AI reply the player reads in the screenshot. |

```
powershell -NoProfile -File tools\screenshots\capture.ps1
```

By default it uses the installed copy in `%LOCALAPPDATA%\Programs\Unmuddle`. Point it at a fresh build with `-Exe out\publish\win-x64\Unmuddle.exe`.

For the Compose shot to show notes, set up an AI helper first. Windows are captured at the scaling of the monitor they open on; a 150% monitor gives sharper images.
