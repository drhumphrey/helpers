# Third-party notices

Helpers is free software under the GNU GPL v3 or later. It is built on, bundles, or downloads the following. The same list is on the Privacy and licences page in Settings, and lives in code in `src/Helpers.Core/ThirdPartyNotices.cs`; keep the three in step.

| Component | Version | Licence | Used for |
|---|---|---|---|
| .NET runtime | 10 | MIT | The platform, bundled with the install |
| Avalonia, with its Fluent theme and colour picker | 12.1.3 | MIT | The user interface |
| sherpa-onnx (includes ONNX Runtime, MIT, and espeak-ng phoneme data, GPL-3.0) | 1.13.8 | Apache-2.0 | Running the voice on this PC |
| Kokoro voice, by hexgrad, with misaki's British pronunciation list | v1.0 | Apache-2.0 | The voice; the word tools' pronunciations |
| NAudio | 3.1.0 | MIT | Playing audio |
| Markdig | 1.4.0 | BSD-2-Clause | Reading Markdown |
| FlaUI | 5.0.0 | MIT | Windows UI Automation, for the selection in other apps |
| LLamaSharp and llama.cpp | 0.27.0 | MIT | Running the local language model |
| Qwen3 4B and 1.7B, GGUF, from Qwen's own repositories | Qwen3 | Apache-2.0 | The local language model |
| Anthropic C# SDK | 12.55.0 | MIT | Claude, when chosen |
| Lexend | | SIL Open Font Licence 1.1 | The reading font |
| Atkinson Hyperlegible, by the Braille Institute | | SIL Open Font Licence 1.1 | An alternative reading font |
| Open English WordNet, built on Princeton WordNet | 2025 | CC BY 4.0 | Meanings and synonyms in the word tools |
| British English hyphenation patterns, by Dominik Wujastyk and Graham Toal, from TeX's hyph-utf8 | 2016 | MIT | Syllables in the word tools |

Parts of Windows the app uses as installed: the spell checker, Credential Manager, UI Automation, the clipboard.

The font licence files are bundled next to the fonts in `src/Helpers.App/Assets/Fonts`. The hyphenation patterns keep their licence header in `src/Helpers.Core/Resources/hyph-en-gb.tex`. The voice, model and dictionary packages keep their own licence files where they are downloaded, under `%LOCALAPPDATA%\Helpers\models`.

Open English WordNet attribution: Open English WordNet 2025, Global WordNet Association, CC BY 4.0, https://github.com/globalwordnet/english-wordnet, derived from Princeton WordNet 3.0, Princeton University.
