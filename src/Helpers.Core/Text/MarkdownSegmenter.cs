using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Helpers.Core.Text;

/// <summary>
/// Turns Markdown into segments the way the brief describes: headings read
/// then pause, bold and code read as plain words, code blocks skipped with a
/// count, lists one item at a time, tables row by row.
/// </summary>
public static class MarkdownSegmenter
{
    public const int PauseAfterHeadingMs = 700;
    public const int PauseAfterParagraphMs = 400;
    public const int PauseAfterListItemMs = 250;
    public const int PauseAfterTableRowMs = 300;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseAutoLinks()
        .Build();

    public static IReadOnlyList<SpeechSegment> Segment(string markdown, ReadingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var document = Markdown.Parse(markdown, Pipeline);
        var segments = new List<SpeechSegment>();
        VisitContainer(document, segments, settings, listPrefix: null);
        return segments;
    }

    private static void VisitContainer(ContainerBlock container, List<SpeechSegment> segments, ReadingSettings settings, string? listPrefix)
    {
        var firstParagraphInItem = listPrefix is not null;

        foreach (var block in container)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    AddText(segments, SegmentKind.Heading, InlineText(heading.Inline), PauseAfterHeadingMs);
                    break;

                case ParagraphBlock paragraph:
                    var text = InlineText(paragraph.Inline);
                    if (firstParagraphInItem)
                    {
                        AddText(segments, SegmentKind.ListItem, listPrefix + text, PauseAfterListItemMs);
                        firstParagraphInItem = false;
                    }
                    else if (IsBoldOnly(paragraph.Inline))
                    {
                        // AI replies use a bold line on its own as a heading. Treat it as one.
                        AddText(segments, SegmentKind.Heading, text, PauseAfterHeadingMs);
                    }
                    else
                    {
                        AddText(segments, SegmentKind.Paragraph, text, PauseAfterParagraphMs);
                    }

                    break;

                case ListBlock list:
                    VisitList(list, segments, settings);
                    break;

                case FencedCodeBlock fenced:
                    AddCodeBlock(segments, fenced, settings);
                    break;

                case CodeBlock code:
                    AddCodeBlock(segments, code, settings);
                    break;

                case Table table:
                    AddTable(segments, table, settings);
                    break;

                case ThematicBreakBlock:
                case HtmlBlock:
                case LinkReferenceDefinitionGroup:
                    break;

                case ContainerBlock other:
                    VisitContainer(other, segments, settings, listPrefix: null);
                    break;

                case LeafBlock leaf when leaf.Inline is not null:
                    AddText(segments, SegmentKind.Paragraph, InlineText(leaf.Inline), PauseAfterParagraphMs);
                    break;
            }
        }
    }

    private static void VisitList(ListBlock list, List<SpeechSegment> segments, ReadingSettings settings)
    {
        var number = list.IsOrdered && int.TryParse(list.OrderedStart, out var start) ? start : 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var prefix = list.IsOrdered ? $"{number}. " : string.Empty;
            VisitContainer(item, segments, settings, prefix);
            number++;
        }
    }

    private static void AddCodeBlock(List<SpeechSegment> segments, CodeBlock code, ReadingSettings settings)
    {
        var lines = code.Lines.Lines
            .Take(code.Lines.Count)
            .Select(l => l.ToString())
            .ToList();

        // Fenced blocks often end with an empty line from the closing fence.
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        var count = lines.Count;
        var caption = count == 1 ? "code block, 1 line" : $"code block, {count} lines";

        switch (settings.CodeBlocks)
        {
            case CodeBlockReading.Skip:
                segments.Add(new SpeechSegment(SegmentKind.CodeBlock, caption, caption, PauseAfterParagraphMs));
                break;

            case CodeBlockReading.FirstLine:
                var first = count > 0 ? lines[0].Trim() : string.Empty;
                var text = first.Length > 0 ? $"{caption}. First line: {first}" : caption;
                segments.Add(new SpeechSegment(SegmentKind.CodeBlock, text, text, PauseAfterParagraphMs));
                break;

            case CodeBlockReading.All:
                segments.Add(new SpeechSegment(SegmentKind.CodeBlock, caption, caption, PauseAfterListItemMs));
                foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var trimmed = line.Trim();
                    segments.Add(new SpeechSegment(SegmentKind.CodeBlock, trimmed, trimmed, PauseAfterListItemMs));
                }

                break;
        }
    }

    private static void AddTable(List<SpeechSegment> segments, Table table, ReadingSettings settings)
    {
        var rows = table.OfType<TableRow>().ToList();
        var header = rows.FirstOrDefault(r => r.IsHeader);
        var body = rows.Where(r => !r.IsHeader).ToList();
        var columnNames = header is null
            ? []
            : header.OfType<TableCell>().Select(CellText).ToList();
        var columnCount = Math.Max(columnNames.Count, body.Count > 0 ? body.Max(r => r.Count) : 0);

        var caption = $"table, {Plural(columnCount, "column")}, {Plural(body.Count, "row")}";
        segments.Add(new SpeechSegment(SegmentKind.TableCaption, caption, caption, PauseAfterParagraphMs));

        if (settings.SkipTables)
        {
            return;
        }

        foreach (var row in body)
        {
            var parts = new List<string>();
            var cells = row.OfType<TableCell>().ToList();
            for (var i = 0; i < cells.Count; i++)
            {
                var value = CellText(cells[i]);
                if (value.Length == 0)
                {
                    continue;
                }

                parts.Add(i < columnNames.Count && columnNames[i].Length > 0 ? $"{columnNames[i]}: {value}" : value);
            }

            if (parts.Count > 0)
            {
                // Semicolons: the voice pauses on them, and the sentence splitter leaves them alone.
                var text = string.Join("; ", parts);
                segments.Add(new SpeechSegment(SegmentKind.TableRow, text, text, PauseAfterTableRowMs));
            }
        }
    }

    private static string CellText(TableCell cell)
    {
        var builder = new StringBuilder();
        foreach (var block in cell)
        {
            if (block is LeafBlock leaf && leaf.Inline is not null)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(InlineText(leaf.Inline));
            }
        }

        return builder.ToString().Trim();
    }

    /// <summary>True when the paragraph is nothing but one bold run, such as <c>**What works**</c>.</summary>
    private static bool IsBoldOnly(ContainerInline? inline) =>
        inline?.FirstChild is EmphasisInline { DelimiterCount: 2, NextSibling: null };

    private static void AddText(List<SpeechSegment> segments, SegmentKind kind, string text, int pauseMs)
    {
        text = text.Trim();
        if (text.Length > 0)
        {
            segments.Add(new SpeechSegment(kind, text, text, pauseMs));
        }
    }

    /// <summary>Flattens inline Markdown to plain words. Bold, italics and code keep their text; symbols go.</summary>
    private static string InlineText(ContainerInline? container)
    {
        var builder = new StringBuilder();
        AppendInlines(container, builder);
        return builder.ToString();
    }

    private static void AppendInlines(ContainerInline? container, StringBuilder builder)
    {
        if (container is null)
        {
            return;
        }

        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;

                case CodeInline code:
                    builder.Append(code.Content);
                    break;

                case LinkInline link:
                    var start = builder.Length;
                    AppendInlines(link, builder);
                    if (builder.Length == start)
                    {
                        builder.Append(link.IsImage ? "image" : link.Url ?? "link");
                    }

                    break;

                case AutolinkInline autolink:
                    builder.Append(autolink.Url);
                    break;

                case LineBreakInline lineBreak:
                    builder.Append(lineBreak.IsHard ? '\n' : ' ');
                    break;

                case HtmlEntityInline entity:
                    builder.Append(entity.Transcoded.ToString());
                    break;

                case HtmlInline:
                    break;

                case ContainerInline nested:
                    AppendInlines(nested, builder);
                    break;
            }
        }
    }

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
