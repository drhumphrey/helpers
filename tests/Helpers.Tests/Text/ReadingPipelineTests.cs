using Helpers.Core.Text;

namespace Helpers.Tests.Text;

/// <summary>
/// End-to-end checks against real Claude Code replies saved under Fixtures.
/// These are the acceptance checks from the brief's Markdown section.
/// </summary>
public class ReadingPipelineTests
{
    private static readonly ReadingSettings Defaults = new();

    public static TheoryData<string> FixtureFiles =>
    [
        "claude-code-reply-milestone-report.md",
        "claude-code-reply-assessment.md",
    ];

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public void NoMarkdownSymbolsAreEverSpoken(string fixture)
    {
        var segments = ReadingPipeline.Prepare(LoadFixture(fixture), Defaults);

        Assert.True(segments.Count > 10, "Expected a reply this long to produce many segments.");
        foreach (var segment in segments)
        {
            Assert.DoesNotContain("**", segment.Speak);
            Assert.DoesNotContain("```", segment.Speak);
            Assert.DoesNotContain("`", segment.Speak);
            Assert.DoesNotContain("|", segment.Speak);
            Assert.DoesNotContain("](", segment.Speak);
            Assert.False(segment.Speak.StartsWith('#'), $"Heading hash spoken: {segment.Speak}");
            Assert.False(segment.Speak.StartsWith('-'), $"Bullet spoken: {segment.Speak}");
        }
    }

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public void NothingHandedToTheEngineIsLongerThanTheLimit(string fixture)
    {
        var segments = ReadingPipeline.Prepare(LoadFixture(fixture), Defaults);

        Assert.All(segments, s => Assert.True(s.Speak.Length <= Defaults.MaxChunkLength, $"Too long: {s.Speak}"));
        Assert.All(segments, s => Assert.False(string.IsNullOrWhiteSpace(s.Speak)));
    }

    [Fact]
    public void MilestoneReportReadsCodeTableAndLinksTheRightWay()
    {
        var segments = ReadingPipeline.Prepare(LoadFixture("claude-code-reply-milestone-report.md"), Defaults);
        var spoken = segments.Select(s => s.Speak).ToList();

        Assert.Contains("code block, 1 line", spoken);
        Assert.Contains("table, 3 columns, 3 rows", spoken);
        Assert.Contains(spoken, s => s.StartsWith("Measure: Model load; Result: under 1 s", StringComparison.Ordinal));
        Assert.Contains(segments, s => s.Kind == SegmentKind.Heading && s.Speak == "What works");
        Assert.Contains(spoken, s => s.StartsWith("The paragraph is written like an AI reply, with \"Dr.\", \"Fig. 2\", \"e.g.\" and version numbers", StringComparison.Ordinal));
        Assert.Contains(spoken, s => s.Contains("Details are in file MILESTONES.md", StringComparison.Ordinal));
        Assert.Contains(spoken, s => s.Contains("the spike lives in file Program.cs", StringComparison.Ordinal));
        Assert.Contains("Milestone 0 is done and pushed.", spoken);
    }

    [Fact]
    public void AssessmentReadsHeadingsNumbersQuotesAndAddresses()
    {
        var segments = ReadingPipeline.Prepare(LoadFixture("claude-code-reply-assessment.md"), Defaults);
        var spoken = segments.Select(s => s.Speak).ToList();

        Assert.Contains(segments, s => s.Kind == SegmentKind.Heading && s.Speak == "What's strong in the brief");
        Assert.Contains(spoken, s => s.StartsWith("1. Reading AI output.", StringComparison.Ordinal));
        Assert.Contains("Build the reading half first.", spoken);
        Assert.Contains(spoken, s => s.Contains("See link for details.", StringComparison.Ordinal));
        Assert.Contains(spoken, s => s.Contains("email address with questions", StringComparison.Ordinal));
        Assert.Contains(spoken, s => s.StartsWith("Version 2.0 shipped on 7 Oct. 2026, e.g. after the Dr. Patel review of Fig. 3.", StringComparison.Ordinal));
        Assert.Contains("code block, 1 line", spoken);
    }

    [Fact]
    public void PlainTextStillFollowsThePrototypeRules()
    {
        var text = "Hi Dave\n\n• Check https://example.com today\n• Reply to sam@example.com\n\nThanks";

        var segments = ReadingPipeline.Prepare(text, Defaults);

        Assert.Equal(["Hi Dave", "Check link today", "Reply to email address", "Thanks"], segments.Select(s => s.Speak));
        Assert.Equal(["Hi Dave", "Check https://example.com today", "Reply to sam@example.com", "Thanks"], segments.Select(s => s.Display));
    }

    [Fact]
    public void SentencesInsideAParagraphGetAShortPauseAndTheLastGetsTheParagraphPause()
    {
        var segments = ReadingPipeline.Prepare("# Title\n\nOne. Two.", Defaults);

        Assert.Equal(3, segments.Count);
        Assert.Equal(MarkdownSegmenter.PauseAfterHeadingMs, segments[0].PauseAfterMs);
        Assert.Equal(ReadingPipeline.PauseBetweenSentencesMs, segments[1].PauseAfterMs);
        Assert.Equal(MarkdownSegmenter.PauseAfterParagraphMs, segments[2].PauseAfterMs);
    }

    [Fact]
    public void WindowsLineEndingsAreHandled()
    {
        var segments = ReadingPipeline.Prepare("One.\r\nTwo.\r\n", Defaults);

        Assert.Equal(["One.", "Two."], segments.Select(s => s.Speak));
    }

    private static string LoadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
