namespace Helpers.Core.Text;

/// <summary>What kind of structure a segment came from. Drives pauses and, later, the reading view.</summary>
public enum SegmentKind
{
    Paragraph,
    Heading,
    ListItem,
    CodeBlock,
    TableCaption,
    TableRow,
}

/// <summary>
/// One unit of reading: the text to show in the reading view, the text to send
/// to the speech engine, and how long to pause once it has been spoken.
/// </summary>
/// <param name="Kind">Where the text came from.</param>
/// <param name="Display">Clean, human-readable text with Markdown symbols removed.</param>
/// <param name="Speak">The same text after the spoken-text rules: links, emails, file paths and pronunciations.</param>
/// <param name="PauseAfterMs">A pause to leave after this segment, in milliseconds.</param>
public sealed record SpeechSegment(SegmentKind Kind, string Display, string Speak, int PauseAfterMs);
