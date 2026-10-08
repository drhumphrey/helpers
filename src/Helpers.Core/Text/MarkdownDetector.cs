using System.Text.RegularExpressions;

namespace Helpers.Core.Text;

/// <summary>
/// Decides whether a piece of text should be read as Markdown. AI chats produce
/// Markdown; emails and documents mostly don't, and they read better as plain text.
/// </summary>
public static partial class MarkdownDetector
{
    public static bool LooksLikeMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var strong = 0;
        if (text.Contains("```", StringComparison.Ordinal)) strong++;
        if (Heading().IsMatch(text)) strong++;
        if (TableSeparatorRow().IsMatch(text)) strong++;
        if (Link().IsMatch(text)) strong++;
        if (strong > 0)
        {
            return true;
        }

        var weak = 0;
        if (BulletItem().IsMatch(text)) weak++;
        if (NumberedItem().IsMatch(text)) weak++;
        if (Bold().IsMatch(text)) weak++;
        if (InlineCode().IsMatch(text)) weak++;
        return weak >= 2;
    }

    [GeneratedRegex(@"^#{1,6}\s+\S", RegexOptions.Multiline)]
    private static partial Regex Heading();

    [GeneratedRegex(@"^\s*\|?\s*:?-{3,}:?\s*\|", RegexOptions.Multiline)]
    private static partial Regex TableSeparatorRow();

    [GeneratedRegex(@"\[[^\]\n]+\]\([^)\n]+\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"^\s*[-*+]\s+\S", RegexOptions.Multiline)]
    private static partial Regex BulletItem();

    [GeneratedRegex(@"^\s*\d+\.\s+\S", RegexOptions.Multiline)]
    private static partial Regex NumberedItem();

    [GeneratedRegex(@"\*\*[^*\n]+\*\*")]
    private static partial Regex Bold();

    [GeneratedRegex(@"`[^`\n]+`")]
    private static partial Regex InlineCode();
}
