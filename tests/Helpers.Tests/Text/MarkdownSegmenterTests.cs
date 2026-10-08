using Helpers.Core.Text;

namespace Helpers.Tests.Text;

public class MarkdownSegmenterTests
{
    private static readonly ReadingSettings Defaults = new();

    [Fact]
    public void HeadingsLoseTheirHashesAndGetALongerPause()
    {
        var result = MarkdownSegmenter.Segment("## What works\n\nA paragraph.", Defaults);

        Assert.Equal(2, result.Count);
        Assert.Equal(SegmentKind.Heading, result[0].Kind);
        Assert.Equal("What works", result[0].Display);
        Assert.Equal(MarkdownSegmenter.PauseAfterHeadingMs, result[0].PauseAfterMs);
        Assert.Equal(SegmentKind.Paragraph, result[1].Kind);
    }

    [Fact]
    public void ABoldLineOnItsOwnIsAHeading()
    {
        var result = MarkdownSegmenter.Segment("**What works**\n\nSome text.\n\n**Short version:** not a heading.", Defaults);

        Assert.Equal(3, result.Count);
        Assert.Equal(SegmentKind.Heading, result[0].Kind);
        Assert.Equal("What works", result[0].Display);
        Assert.Equal(MarkdownSegmenter.PauseAfterHeadingMs, result[0].PauseAfterMs);
        Assert.Equal(SegmentKind.Paragraph, result[2].Kind);
        Assert.Equal("Short version: not a heading.", result[2].Display);
    }

    [Fact]
    public void BoldItalicAndInlineCodeKeepTheirWordsOnly()
    {
        var result = MarkdownSegmenter.Segment("**Short version:** run `dotnet build` and *listen*.", Defaults);

        Assert.Equal("Short version: run dotnet build and listen.", Assert.Single(result).Display);
    }

    [Fact]
    public void CodeBlocksAreSkippedWithACountByDefault()
    {
        var result = MarkdownSegmenter.Segment("Run:\n\n```powershell\nwinget install X\ndotnet build\n```\n\nDone.", Defaults);

        Assert.Equal(["Run:", "code block, 2 lines", "Done."], result.Select(s => s.Display));
        Assert.Equal(SegmentKind.CodeBlock, result[1].Kind);
    }

    [Fact]
    public void SingleLineCodeBlockIsSingular()
    {
        var result = MarkdownSegmenter.Segment("```\ngit fetch origin\n```", Defaults);

        Assert.Equal("code block, 1 line", Assert.Single(result).Display);
    }

    [Fact]
    public void CodeBlocksCanReadTheFirstLine()
    {
        var settings = new ReadingSettings { CodeBlocks = CodeBlockReading.FirstLine };

        var result = MarkdownSegmenter.Segment("```\ngit fetch origin\ngit push\n```", settings);

        Assert.Equal("code block, 2 lines. First line: git fetch origin", Assert.Single(result).Display);
    }

    [Fact]
    public void CodeBlocksCanBeReadInFull()
    {
        var settings = new ReadingSettings { CodeBlocks = CodeBlockReading.All };

        var result = MarkdownSegmenter.Segment("```\ngit fetch origin\n\ngit push\n```", settings);

        Assert.Equal(["code block, 3 lines", "git fetch origin", "git push"], result.Select(s => s.Display));
    }

    [Fact]
    public void BulletListsBecomeOneSegmentPerItem()
    {
        var result = MarkdownSegmenter.Segment("- First point\n- Second **bold** point\n- Third", Defaults);

        Assert.Equal(["First point", "Second bold point", "Third"], result.Select(s => s.Display));
        Assert.All(result, s => Assert.Equal(SegmentKind.ListItem, s.Kind));
        Assert.All(result, s => Assert.Equal(MarkdownSegmenter.PauseAfterListItemMs, s.PauseAfterMs));
    }

    [Fact]
    public void NumberedListsSayTheirNumbers()
    {
        var result = MarkdownSegmenter.Segment("1. Install it\n2. Restart\n3. Build", Defaults);

        Assert.Equal(["1. Install it", "2. Restart", "3. Build"], result.Select(s => s.Display));
    }

    [Fact]
    public void NumberedListsRespectTheStartingNumber()
    {
        var result = MarkdownSegmenter.Segment("4. Fourth\n5. Fifth", Defaults);

        Assert.Equal(["4. Fourth", "5. Fifth"], result.Select(s => s.Display));
    }

    [Fact]
    public void ListItemsWithExtraParagraphsReadThemAsParagraphs()
    {
        var result = MarkdownSegmenter.Segment("- Item\n\n  More about the item.\n- Next", Defaults);

        Assert.Equal(["Item", "More about the item.", "Next"], result.Select(s => s.Display));
        Assert.Equal(SegmentKind.Paragraph, result[1].Kind);
    }

    [Fact]
    public void TablesGetACaptionAndAreReadRowByRow()
    {
        var markdown = "| Measure | Result |\n|---|---|\n| Model load | under 1 s |\n| Memory | 450 MB |";

        var result = MarkdownSegmenter.Segment(markdown, Defaults);

        Assert.Equal(
            ["table, 2 columns, 2 rows", "Measure: Model load; Result: under 1 s", "Measure: Memory; Result: 450 MB"],
            result.Select(s => s.Display));
        Assert.Equal(SegmentKind.TableCaption, result[0].Kind);
        Assert.Equal(SegmentKind.TableRow, result[1].Kind);
    }

    [Fact]
    public void TablesCanBeSkippedAfterTheCaption()
    {
        var settings = new ReadingSettings { SkipTables = true };
        var markdown = "| a | b |\n|---|---|\n| 1 | 2 |";

        var result = MarkdownSegmenter.Segment(markdown, settings);

        Assert.Equal("table, 2 columns, 1 row", Assert.Single(result).Display);
    }

    [Fact]
    public void LinksReadTheirTextAndBareLinksKeepTheAddressForTheUrlRule()
    {
        var result = MarkdownSegmenter.Segment("See [the brief](docs/BRIEF.md) or <https://example.com>.", Defaults);

        Assert.Equal("See the brief or https://example.com.", Assert.Single(result).Display);
    }

    [Fact]
    public void ImagesSayImage()
    {
        var result = MarkdownSegmenter.Segment("Look: ![](shot.png)", Defaults);

        Assert.Equal("Look: image", Assert.Single(result).Display);
    }

    [Fact]
    public void HorizontalRulesAndHtmlAreSkippedAndQuotesAreRead()
    {
        var result = MarkdownSegmenter.Segment("Before.\n\n---\n\n<div>hidden</div>\n\n> Build the reading half first.\n\nAfter.", Defaults);

        Assert.Equal(["Before.", "Build the reading half first.", "After."], result.Select(s => s.Display));
    }

    [Fact]
    public void HardLineBreaksBecomeNewLinesInsideAParagraph()
    {
        var result = MarkdownSegmenter.Segment("Line one  \nLine two", Defaults);

        Assert.Equal("Line one\nLine two", Assert.Single(result).Display);
    }
}
