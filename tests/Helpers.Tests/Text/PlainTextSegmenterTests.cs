using Helpers.Core.Text;

namespace Helpers.Tests.Text;

public class PlainTextSegmenterTests
{
    [Fact]
    public void EachLineBecomesASegmentAndBlankLinesAreDropped()
    {
        var result = PlainTextSegmenter.Segment("First line.\n\n\nSecond line\n   \nThird.");

        Assert.Equal(["First line.", "Second line", "Third."], result.Select(s => s.Display));
        Assert.All(result, s => Assert.Equal(SegmentKind.Paragraph, s.Kind));
        Assert.All(result, s => Assert.Equal(PlainTextSegmenter.PauseAfterLineMs, s.PauseAfterMs));
    }

    [Theory]
    [InlineData("• A bullet")]
    [InlineData("- A bullet")]
    [InlineData("* A bullet")]
    [InlineData("· A bullet")]
    [InlineData("  – A bullet")]
    public void StripsLeadingBulletCharacters(string line)
    {
        var result = PlainTextSegmenter.Segment(line);

        Assert.Equal("A bullet", Assert.Single(result).Display);
    }

    [Fact]
    public void KeepsDashesInsideText()
    {
        var result = PlainTextSegmenter.Segment("Read-aloud is a two-part job - mostly.");

        Assert.Equal("Read-aloud is a two-part job - mostly.", Assert.Single(result).Display);
    }
}
