namespace Helpers.Core.Text;

/// <summary>
/// The whole journey from captured text to engine-ready sentences:
/// detect Markdown, break into structural segments, split into sentences,
/// then apply the spoken-text rules to each one.
/// </summary>
public static class ReadingPipeline
{
    /// <summary>Pause between sentences that belong to the same paragraph.</summary>
    public const int PauseBetweenSentencesMs = 150;

    public static IReadOnlyList<SpeechSegment> Prepare(string text, ReadingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        var structural = MarkdownDetector.LooksLikeMarkdown(text)
            ? MarkdownSegmenter.Segment(text, settings)
            : PlainTextSegmenter.Segment(text);

        var result = new List<SpeechSegment>();
        foreach (var segment in structural)
        {
            var sentences = segment.Kind == SegmentKind.CodeBlock
                ? [segment.Display]
                : SentenceSplitter.Split(segment.Display, settings.MaxChunkLength);

            for (var i = 0; i < sentences.Count; i++)
            {
                var display = sentences[i];
                var speak = SpokenTextRules.ToSpeech(display, settings);
                if (speak.Length == 0)
                {
                    continue;
                }

                var last = i == sentences.Count - 1;
                result.Add(new SpeechSegment(segment.Kind, display, speak, last ? segment.PauseAfterMs : PauseBetweenSentencesMs));
            }
        }

        return result;
    }
}
