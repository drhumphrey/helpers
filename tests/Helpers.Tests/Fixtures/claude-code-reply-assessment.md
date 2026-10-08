**Short version:** the brief is good and I'd happily build from it. But it describes about a third of what you just told me you need. It is a read-aloud app. You described a dyslexia companion for working with AI chat.

## What's strong in the brief

- **The hard Windows problems are already solved on paper.** Non-activating windows, clipboard restore, the hook thread, UI Automation time-boxing, password fields.
- **Offline neural voice is the right call.** Kokoro via sherpa-onnx is Apache-2.0, free forever, no tokens.
- **Your AutoHotkey prototype is a solid reference.** The text tidy function and the Ctrl+Insert trick for terminals should be ported as written.

## What's missing

1. **Reading AI output.** The clean-up rules are tuned for email. AI chat output is Markdown: code fences, backticks, hash headers, bold stars, file paths, tables.
2. **Nothing for writing.** No spelling, no grammar, no restructuring. The brief is read-only.
3. **Terminal risk.** VS Code's built-in terminal treats Ctrl+C with nothing selected as "interrupt". That could kill a running Claude Code task.
4. **Wrong model ID.** The brief names a Haiku model that doesn't exist. The current one is `claude-haiku-4-5`.

Tidy needs a language model. Two options, both behind one interface:

- **Local small model** through LLamaSharp, which is MIT licensed. A 3 to 4 billion parameter model runs on CPU and nothing leaves the PC. See https://github.com/SciSharp/LLamaSharp for details.
- **Bring your own key** for Claude or another provider. The app stays free. Off by default.

> Build the reading half first. It is useful on day one by itself.

---

Install the SDK with:

```powershell
winget install Microsoft.DotNet.SDK.10
```

Then open `src/Helpers.Core/Text/ReadingPipeline.cs` and `docs/BRIEF.md`, or email david@example.com with questions. Version 2.0 shipped on 7 Oct. 2026, e.g. after the Dr. Patel review of Fig. 3.
