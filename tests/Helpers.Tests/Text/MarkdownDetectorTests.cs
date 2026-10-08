using Helpers.Core.Text;

namespace Helpers.Tests.Text;

public class MarkdownDetectorTests
{
    [Theory]
    [InlineData("Run this:\n```\ndotnet build\n```")]
    [InlineData("# A heading\n\nSome text.")]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |")]
    [InlineData("See [the brief](docs/BRIEF.md) for details.")]
    [InlineData("- one thing\n- another thing\n\nWith **bold** too.")]
    public void RecognisesMarkdown(string text)
    {
        Assert.True(MarkdownDetector.LooksLikeMarkdown(text));
    }

    [Theory]
    [InlineData("Hi Dave,\n\nThanks for the update. See you Monday.\n\nBest,\nSam")]
    [InlineData("A single dash - in prose is not a list.")]
    [InlineData("- just one bullet line on its own")]
    [InlineData("Multiply 3 * 4 and then 5 * 6.")]
    [InlineData("")]
    public void LeavesPlainTextAlone(string text)
    {
        Assert.False(MarkdownDetector.LooksLikeMarkdown(text));
    }
}
