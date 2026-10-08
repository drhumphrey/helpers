using Helpers.Core.Text;

namespace Helpers.Tests.Text;

public class SentenceSplitterTests
{
    [Fact]
    public void SplitsOnFullStopsQuestionsAndExclamations()
    {
        var result = SentenceSplitter.Split("First one. Second one? Third one! Fourth.");

        Assert.Equal(["First one.", "Second one?", "Third one!", "Fourth."], result);
    }

    [Theory]
    [InlineData("Dr. Patel's note is unrelated. Next sentence.", 2)]
    [InlineData("See Smith et al. for details. Next sentence.", 2)]
    [InlineData("It concerns the timeout, e.g. the slow one. Next sentence.", 2)]
    [InlineData("Look at Fig. 2 before reading on. Next sentence.", 2)]
    [InlineData("Ask Mr. Jones or Mrs. Smith. Next sentence.", 2)]
    [InlineData("With \"Dr.\", \"Fig. 2\", \"e.g.\" and numbers in it. Next sentence.", 2)]
    public void DoesNotSplitAfterAbbreviations(string text, int expectedCount)
    {
        Assert.Equal(expectedCount, SentenceSplitter.Split(text).Count);
    }

    [Theory]
    [InlineData("It takes 2.5 seconds to load. Then it runs.", 2)]
    [InlineData("Version v2.0 shipped. Version 10.0.401 is next.", 2)]
    [InlineData("Open Program.cs and README.md first. Then build.", 2)]
    [InlineData("Change it to net10.0. Then run again.", 2)]
    public void DoesNotSplitInsideNumbersVersionsOrFileNames(string text, int expectedCount)
    {
        Assert.Equal(expectedCount, SentenceSplitter.Split(text).Count);
    }

    [Fact]
    public void KeepsListNumbersWithTheirItem()
    {
        var result = SentenceSplitter.Split("1. Install the SDK. Then restart.");

        Assert.Equal(["1. Install the SDK.", "Then restart."], result);
    }

    [Fact]
    public void KeepsClosingQuotesWithTheSentence()
    {
        var result = SentenceSplitter.Split("He said \"stop.\" Then he left.");

        Assert.Equal(["He said \"stop.\"", "Then he left."], result);
    }

    [Fact]
    public void TreatsLineBreaksAsBoundaries()
    {
        var result = SentenceSplitter.Split("No punctuation here\nAnd a second line.");

        Assert.Equal(["No punctuation here", "And a second line."], result);
    }

    [Fact]
    public void ChunksLongSentencesAtCommasBeforeTheLimit()
    {
        var clause = "this clause has about forty characters, ";
        var text = string.Concat(Enumerable.Repeat(clause, 15)).TrimEnd(' ', ',') + ".";

        var result = SentenceSplitter.Split(text, maxLength: 100);

        Assert.True(result.Count > 1);
        Assert.All(result, s => Assert.True(s.Length <= 100, $"Too long: {s.Length}"));
        Assert.All(result.SkipLast(1), s => Assert.EndsWith(",", s));
    }

    [Fact]
    public void ReturnsNothingForBlankInput()
    {
        Assert.Empty(SentenceSplitter.Split("   \n  "));
    }
}
