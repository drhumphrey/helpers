using System.Text.RegularExpressions;

namespace Helpers.Core.Text;

/// <summary>
/// Plain-text reading rules, ported from the AutoHotkey prototype: bullet
/// characters are stripped, blank lines are dropped, and every line becomes
/// its own segment so a line break with no punctuation still gives a pause.
/// </summary>
public static partial class PlainTextSegmenter
{
    public const int PauseAfterLineMs = 300;

    public static IReadOnlyList<SpeechSegment> Segment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var segments = new List<SpeechSegment>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = LeadingBullet().Replace(rawLine.Trim(), string.Empty).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            segments.Add(new SpeechSegment(SegmentKind.Paragraph, line, line, PauseAfterLineMs));
        }

        return segments;
    }

    [GeneratedRegex(@"^[•◦▪·–—*-]+\s+")]
    private static partial Regex LeadingBullet();
}
