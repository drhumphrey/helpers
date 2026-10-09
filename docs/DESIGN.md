# Helpers: design

How the app looks and behaves on screen. The first mockups were drawn on a private design canvas, one artboard per surface in light and dark; the app has since moved past them, and screenshots of what it looks like now are in [images/](images/).

## The look in one line

One set of shapes, three skins. The shapes: one translucent surface style, large type, nothing clickable under 32 px, nothing on screen that isn't doing a job right now. The skins, called vibes: Neon (gradient edges, glow, flecks of colour; the default), Custom (your own gradient), and Calm (the quiet glass look with one flat accent, one click away). See Vibes below.

## Decisions proposed here, for Dave to confirm

| Area | Decision |
|---|---|
| UI font | Lexend, bundled, for the app's own controls and labels as well as for reading text. One family keeps the surfaces coherent. The Avalonia template's Inter package can be dropped. |
| Reading font | The user's choice in Settings: system default, Lexend or Atkinson Hyperlegible. Default Lexend. |
| Accent | In Calm, the OS accent colour. In Neon and Custom, a two or three stop gradient; "Follow the OS accent" builds the gradient from that one colour. |
| Surfaces | Translucent (Mica or acrylic on Windows 11, vibrancy on Mac) with a solid fallback on Windows 10. Radius 12 px, pill 999 px. One soft shadow. |
| Theme | Follows the OS light/dark setting by default, with an override in Settings, because the Neon vibe wants dark. |
| Errors | Only ever as toasts. No dialogs anywhere. |

## Tokens

| Token | Light | Dark |
|---|---|---|
| Ground (desktop, settings page) | #EEF1F5 | #15171C |
| Surface (overlays, windows) | white at 86 to 94% with 18 px blur | #22252C at 88 to 96% with 18 px blur |
| Solid (inputs, menus) | #FFFFFF | #22252C |
| Line (borders, dividers) | black at 12% | white at 12% |
| Text | #1B1F24 | #ECEFF3 |
| Muted text | #5B6572 | #A2ABB6 |
| Accent (stand-in) | #0F6CBD | #6BB2FF |
| On accent | #FFFFFF | #0B1C33 |
| Current sentence highlight | #FFE89A, text #1B1F24 | amber at 26%, text #FFE9A8 |
| Selection (mock only) | accent at 22% | accent at 30% |
| Error | #B42318 | #FF8A80 |
| Success | #1B7F4B | #6EDFA3 |
| Removed words (diff) | #B42318 on red at 10% | #FF8A80 on red at 14% |
| Added words (diff) | #1B7F4B on green at 12% | #6EDFA3 on green at 14% |
| Shadow | 0 12 32 black 14%, plus 0 2 6 black 8% | 0 12 32 black 50%, plus 0 2 6 black 35% |

Every text colour above passes 4.5:1 on its intended background. Muted text is the one to watch: never put it on the accent.

### Type

| Use | Size | Weight | Line height |
|---|---|---|---|
| Window title, first-run heading | 26 | 600 | 1.2 |
| Section heading in the reading view | 22 | 600 | 1.3 |
| Reading text, Compose editor, AI results | 20 | 400 | 1.6 |
| Current sentence in the compact player | 18 | 400 | 1.45 |
| Controls, labels, menu items, settings rows | 15 | 400 or 500 | 1.3 |
| Hints and captions | 13 or 14 | 400 | 1.4 |
| Section labels | 13, upper case, 0.04 em tracking | 400 | 1 |

Reading text size, line height, font and background tint are user settings and apply everywhere the user's own text appears.

### Shape, space, motion

- Radius: 12 px on surfaces and windows, 10 px on buttons, 999 px on the pill and voice chips, 6 px on the sentence highlight.
- Padding: 12 to 14 px inside bars, 18 to 26 px inside panels. Gaps of 8 to 12 px between controls.
- Targets: icon buttons 40 px, the pill's buttons 40 px tall, the player's primary button 48 px, settings toggles 44 by 24 px. Nothing under 32 px.
- Motion: fades and slides of 150 ms or less, ease-out, no bounce. Honour the OS reduced-motion setting by cutting all of it.
- Icons: Fluent UI System Icons (MIT), stroke style, 18 px in buttons, 22 px for the primary control. The mockups use hand-drawn stand-ins of the same weight.
- Sound: none.

## Surfaces

| Surface | Window type | Appears | Position | Goes away |
|---|---|---|---|---|
| Read pill | Non-activating, topmost, no taskbar entry | Within 200 ms of mouse-up after a selection | Just below the end of the selection, on the cursor's monitor | 3 s, or any click, key or scroll elsewhere |
| Player, compact | Non-activating, topmost, draggable | When reading starts | Where it was last left | A few seconds after reading ends (setting), Stop, or Hide |
| Reading view | The player, expanded | Expand button | Grows from the player's position | Collapse, or with the player |
| Toast | Non-activating, topmost | On any message | Above the tray, stacked, newest at the bottom | 3 s for confirmations; errors on click; progress when done |
| AI result card | Non-activating, topmost | After Summarise or Explain simply | Near the cursor, or beside Compose | Use this, Close, or Esc |
| Compose | Normal window | Tray, hotkey or pill | Centre of the cursor's monitor, then where it was last left | Close or Send |
| Settings | Normal window | Tray | Centre, then last position | Close |
| First run | Normal window | First launch only | Centre | Done |

Rules that apply to all of them:

- Esc dismisses whichever surface is on top.
- A toast may carry one action button, never two. Never more than three toasts; older ones collapse.
- Overlays never steal focus. Only Compose, Settings and First run take it, because they hold a text box or a list.
- Colour is never the only signal. A red circle has words next to it; a green tick has words next to it.
- The pill shows Summarise only when an AI helper is set up. Anything that would send text off the machine carries the cloud icon and the words "Text left this PC" on its result.

### Anatomy notes

- **Pill:** Read in the accent colour, Summarise plain, one rounded capsule. Read is first because it is the one clicked a hundred times a day.
- **Player:** Pause/Play 48 px in the accent, then back, forward, stop at 40 px; the current sentence fills the middle at 18 px on the highlight colour; speed slider 0.5 to 2.0 in 0.1 steps with the value beside it; voice chip; expand; hide.
- **Reading view:** Same bar on top plus "Sentence 6 of 19". Below, the whole cleaned text at reading size. Read sentences in muted colour, the current one highlighted, the rest in full text colour. Headings larger, list items with a dot, code blocks as a dashed placeholder chip, tables as a caption line then one row per line. Click any sentence to jump there.
- **Toast:** Icon, one line of text, optional action button in the accent colour, close cross on errors. Progress toasts add a bar and a second muted line.
- **AI result card:** Title, provider badge, change count or cost. Two columns for Tidy and Make a request (Yours, Tidied), one column for Summarise and Explain simply. Removed words struck through in red, added words in green. Use this (accent), Keep mine, Read it.
- **Compose:** Title bar with "Sending to: app, site" and "Use this window". Editor at reading size with wavy red underlines and a right-click popover (suggestions, Add to dictionary, Ignore). Bottom bar: Read back, Tidy, Make a request on the left; Copy and Send to chat (accent) on the right. Status line: model state, cloud state, draft saved, a reminder that Win+H dictates. With no AI helper, Tidy and Make a request are greyed with a one-line hint.
- **Settings:** Left list of eight pages, one page at a time, rows with a one-line hint under each label. Settings save as they change. The AI page says in plain words what leaves the machine and shows cost in money.
- **First run:** One screen, two choices: pick a voice (eight chips, each speaks when picked) and confirm the Read button and Start with Windows. Done.
- **Tray menu:** Read clipboard, Watch clipboard, Pause, Stop, Show player, Compose, Settings, Pause the Read button for 1 hour, Start with Windows, Exit. Toggles carry a tick.

## Mapping to Avalonia

- One `ResourceDictionary` per theme holding the tokens above as brushes and sizes, selected with `RequestedThemeVariant` following the OS.
- Surfaces: a `Window` with `TransparencyLevelHint` set to Mica, then AcrylicBlur, then Transparent, then None, `SystemDecorations="None"`, `CanResize="False"` for overlays, content in a `Border` with `CornerRadius="12"` and `BoxShadow`.
- Non-activating overlays: after the window is created, take the Win32 handle from `TryGetPlatformHandle()` and set `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`; `ShowActivated="False"`; answer `WM_MOUSEACTIVATE` with `MA_NOACTIVATE`. This is the first spike of milestone 3.
- Tray: Avalonia's own `TrayIcon` with a `NativeMenu`.
- Spelling underlines: an adorner over the `TextBox`, driven by the Windows `ISpellChecker` COM API. The first spike of milestone 8.
- Suggestions popover and the voice list: `Flyout`.
- Toasts: a single always-present non-activating window that lays out up to three items in a `StackPanel`, rather than one window per toast.

## Accessibility checks before each milestone demo

- Every text colour at 4.5:1 or better on its real background, both themes.
- Tab reaches every control in Compose, Settings and First run; overlays are mouse-first but Esc always works.
- Every icon-only button has an accessible name.
- Nothing clickable under 32 px.
- Reduced motion switches every animation off.

## Open items

- The product name, and with it the tray icon. "Helpers" is the working title.
- Whether the compact player should show the next sentence faintly under the current one. Try it at milestone 3.

## Vibes

Dave's steer on 8 October 2026: the layout and elements are right, but the default should be fun, in the spirit of an RGB gaming keyboard, with a way to mute it for professional settings. So the look is split into shapes, which never change, and a skin, which the user picks.

| Vibe | Who it's for | Gradient | Glow | Flecks | Theme |
|---|---|---|---|---|---|
| **Neon** (default) | Anyone who likes their keyboard to light up | Cyan #19E6FF to magenta #FF2FD1 to yellow #FFE14D | On, 60% | On, 35% | Dark |
| **Custom** | People who want their own colours | Two or three colours the user picks, or built from the OS accent | User's choice | User's choice | User's choice |
| **Calm** | Offices, screen sharing, quieter days | None. One flat accent from the OS | Off | Off | Follows the OS |

### What the gradient may touch

Borders of surfaces and the pill (a one-pixel gradient border with a soft outer glow), primary buttons (gradient fill with near-black text, which passes 8:1 or better on every stop), progress bars, the list dots in the reading view, and the current-sentence highlight (the gradient at a quarter strength under white text).

### What it may never touch

Reading text, labels, hints, the Compose editor, the settings pages, toast text. These stay plain text on a plain surface in every vibe, at 7:1 or better. Flecks live on the ground and around surface edges, never over words. Misspelling underlines stay red in every vibe.

### Flecks

Small dots, 3 to 6 px, in the gradient colours plus lime #B6FF3B, with a soft glow, scattered on the ground behind surfaces and along their edges. They drift slowly. Density is a slider from 0 to 100. Reduced motion freezes them; Calm removes them.

### Settings: Look page

Vibe (Neon, Custom, Calm), gradient colours with "Follow the OS accent", glow slider, flecks slider, theme (Follow OS, Light, Dark). Changes apply live. The tray menu gains a "Calm look" toggle so the quiet skin is one click away before a screen share.

### Mapping to Avalonia

- Vibe tokens are a second `ResourceDictionary` layered over the theme one: `GradientBrush` (a `LinearGradientBrush` with the chosen stops), `GlowShadow` (a `BoxShadows` value), `FleckDensity` (a double), `HighlightBrush`.
- Gradient borders: a `Border` with `BorderBrush` set to the gradient brush and `BorderThickness="1"`, around an inner `Border` with the surface brush and the same radius less one.
- Glow: `BoxShadow` on the outer border, two shadows in the two end colours at low alpha. Zero in Calm.
- Flecks: one `Canvas` per surface window behind the content, `Ellipse` children with a `DropShadowEffect`, positions seeded per window, drifting with a slow looping `Animation`; stopped when the OS reports reduced motion.
- Switching vibe swaps the vibe dictionary at runtime; no restart.
