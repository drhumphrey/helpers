namespace Helpers.Core;

/// <summary>One component this app ships or downloads, for the credits page and the notices file.</summary>
public sealed record ThirdPartyComponent(string Name, string Version, string Licence, string Role, string Url, bool UsesNetwork, string NetworkNote);

/// <summary>
/// Everything that is not ours, with its licence and whether it ever touches
/// the network. Shown in Settings and written out as THIRD-PARTY-NOTICES.md.
/// Keep this list in step with the project files and the model downloads.
/// </summary>
public static class ThirdPartyNotices
{
    public static readonly ThirdPartyComponent[] Components =
    [
        new(".NET runtime", "10", "MIT", "The platform the app runs on, bundled with the install.", "https://github.com/dotnet/runtime", false, "None."),
        new("Avalonia", "12.1.3", "MIT", "The user interface, with its Fluent theme and colour picker.", "https://github.com/AvaloniaUI/Avalonia", false, "None."),
        new("sherpa-onnx", "1.13.8", "Apache-2.0", "Runs the voice on this PC. Includes ONNX Runtime (MIT) and espeak-ng's phoneme data (GPL-3.0).", "https://github.com/k2-fsa/sherpa-onnx", false, "None. The voice package is downloaded once from the sherpa-onnx GitHub release over HTTPS."),
        new("Kokoro voice", "v1.0", "Apache-2.0", "The voice itself, by hexgrad, with its British pronunciation list from misaki.", "https://huggingface.co/hexgrad/Kokoro-82M", false, "None once downloaded."),
        new("NAudio", "3.1.0", "MIT", "Plays the audio.", "https://github.com/naudio/NAudio", false, "None."),
        new("Markdig", "1.4.0", "BSD-2-Clause", "Reads Markdown so it can be spoken sensibly.", "https://github.com/xoofx/markdig", false, "None."),
        new("FlaUI", "5.0.0", "MIT", "Asks the app in front for its selected text through Windows UI Automation.", "https://github.com/FlaUI/FlaUI", false, "None."),
        new("LLamaSharp and llama.cpp", "0.27.0", "MIT", "Runs the local language model on this PC's processor.", "https://github.com/SciSharp/LLamaSharp", false, "None. The model file is downloaded once from Hugging Face over HTTPS and checked against a built-in SHA-256."),
        new("Qwen3 models", "4B and 1.7B", "Apache-2.0", "The local language model, in GGUF form from Qwen's own repositories.", "https://huggingface.co/Qwen/Qwen3-4B-GGUF", false, "None once downloaded."),
        new("Anthropic C# SDK", "12.55.0", "MIT", "Talks to Claude when, and only when, you choose Claude and press a button with the cloud mark.", "https://github.com/anthropics/anthropic-sdk-csharp", true, "Sends your draft and the instructions for that button to api.anthropic.com over HTTPS, with your own key. The address is pinned in the app."),
        new("Lexend", "", "SIL Open Font Licence 1.1", "The reading font.", "https://www.lexend.com", false, "None."),
        new("Atkinson Hyperlegible", "", "SIL Open Font Licence 1.1", "An alternative reading font, by the Braille Institute.", "https://www.brailleinstitute.org/freefont", false, "None."),
        new("Open English WordNet", "2025", "CC BY 4.0", "Meanings and synonyms for the word tools. Built on Princeton WordNet.", "https://github.com/globalwordnet/english-wordnet", false, "None. Downloaded once from its GitHub release over HTTPS and checked against a built-in SHA-256."),
        new("British English hyphenation patterns", "2016", "MIT", "Splits words into syllables for the word tools. By Dominik Wujastyk and Graham Toal, from the TeX hyph-utf8 collection.", "https://github.com/hyphenation/tex-hyphen", false, "None. Bundled."),
    ];

    /// <summary>Parts of Windows the app leans on. Not third-party in the licensing sense, but worth saying.</summary>
    public static readonly string[] WindowsParts =
    [
        "The Windows spell checker, the same one Edge and Notepad use, for spelling in Compose.",
        "Windows Credential Manager, for the Claude API key.",
        "UI Automation and the clipboard, for reading the selection in other apps.",
    ];
}
