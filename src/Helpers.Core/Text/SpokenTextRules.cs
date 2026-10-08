using System.Text.RegularExpressions;

namespace Helpers.Core.Text;

/// <summary>
/// Turns display text into something a voice can say well: web addresses become
/// "link", email addresses become "email address", file paths are shortened,
/// and the pronunciation dictionary is applied.
/// </summary>
public static partial class SpokenTextRules
{
    public static string ToSpeech(string display, ReadingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(display))
        {
            return string.Empty;
        }

        var text = display;
        if (settings.SayLinkForUrls)
        {
            text = ReplaceUrls(text);
        }

        if (settings.SayEmailAddress)
        {
            text = ReplaceEmails(text);
        }

        text = ReplaceFilePaths(text, settings.FilePaths);
        if (settings.SayYearsNaturally)
        {
            text = NumberSpeech.YearsAsSpeech(text);
        }

        text = settings.Pronunciations.Apply(text);
        return CollapseWhitespace(text);
    }

    /// <summary>Replaces web addresses with "link", keeping any punctuation that followed them.</summary>
    public static string ReplaceUrls(string text) => UrlPattern().Replace(text, "link");

    public static string ReplaceEmails(string text) => EmailPattern().Replace(text, "email address");

    /// <summary>
    /// Shortens things that look like file paths. A path needs either a file
    /// extension on its last part or at least two separators, so "and/or" and
    /// dates like 8/10/2026 are left alone.
    /// </summary>
    public static string ReplaceFilePaths(string text, FilePathReading mode)
    {
        if (mode == FilePathReading.Full)
        {
            return text;
        }

        return FilePathPattern().Replace(text, match =>
        {
            var path = match.Value;
            var lastSeparator = path.LastIndexOfAny(['/', '\\']);
            var fileName = lastSeparator >= 0 ? path[(lastSeparator + 1)..] : path;
            return mode == FilePathReading.JustSayFile || fileName.Length == 0
                ? "file"
                : "file " + fileName;
        });
    }

    public static string CollapseWhitespace(string text) => WhitespaceRun().Replace(text, " ").Trim();

    [GeneratedRegex(@"\b(?:https?://|www\.)[^\s<>()""']+?(?=[.,;:!?)\]]*(?:\s|$))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(?:\.[\w-]+)+", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    // Either: parts/with/two separators, or one separator followed by name.ext (1-5 letter extension).
    // Must contain a letter somewhere, so dates and fractions don't match.
    [GeneratedRegex(
        @"(?<![\w/\\:.-])(?=[^\s]*[A-Za-z])(?:[A-Za-z]:[/\\]|\.{1,2}[/\\]|~[/\\])?(?:(?:[\w.-]+[/\\]){2,}[\w.-]*|[\w.-]+[/\\][\w-]+(?:\.[\w-]+)*\.[A-Za-z]{1,5})(?![\w/\\])",
        RegexOptions.CultureInvariant)]
    private static partial Regex FilePathPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
