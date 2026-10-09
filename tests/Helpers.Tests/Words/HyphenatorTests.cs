using Helpers.Core.Words;

namespace Helpers.Tests.Words;

public class HyphenatorTests
{
    [Fact]
    public void LoadsTheBritishPatterns()
    {
        Assert.True(Hyphenator.British.PatternCount > 8000);
    }

    [Theory]
    [InlineData("registration", "re·gis·tra·tion")]
    [InlineData("university", "uni·ver·sity")]
    [InlineData("however", "how·ever")]
    [InlineData("information", "in·form·a·tion")]
    [InlineData("necessary", "ne·ces·sary")]
    public void SplitsLongWordsIntoPieces(string word, string expected)
    {
        Assert.Equal(expected, Hyphenator.British.Hyphenate(word));
    }

    [Theory]
    [InlineData("cat")]
    [InlineData("the")]
    [InlineData("I")]
    public void ShortWordsStayWhole(string word)
    {
        Assert.Equal([word], Hyphenator.British.Syllables(word));
    }

    [Fact]
    public void KeepsTheOriginalCase()
    {
        var pieces = Hyphenator.British.Syllables("Registration");

        Assert.Equal("Registration", string.Concat(pieces));
        Assert.StartsWith("Re", pieces[0]);
        Assert.True(pieces.Count >= 3);
    }

    [Fact]
    public void WordsWithOddCharactersComeBackWhole()
    {
        Assert.Equal(["e-mail"], Hyphenator.British.Syllables("e-mail"));
        Assert.Empty(Hyphenator.British.Syllables("  "));
    }
}
