using Helpers.Core.Text;

namespace Helpers.Tests.Text;

public class NumberSpeechTests
{
    [Theory]
    [InlineData(1995, "nineteen ninety-five")]
    [InlineData(1900, "nineteen hundred")]
    [InlineData(1905, "nineteen oh five")]
    [InlineData(1066, "ten sixty-six")]
    [InlineData(1500, "fifteen hundred")]
    [InlineData(2000, "two thousand")]
    [InlineData(2005, "two thousand and five")]
    [InlineData(2010, "twenty ten")]
    [InlineData(2026, "twenty twenty-six")]
    [InlineData(1812, "eighteen twelve")]
    public void SaysYearsTheWayPeopleDo(int year, string expected)
    {
        Assert.Equal(expected, NumberSpeech.SayYear(year));
    }

    [Theory]
    [InlineData("Founded in 1995 and sold in 2026.", "Founded in nineteen ninety-five and sold in twenty twenty-six.")]
    [InlineData("The 1995-2001 period.", "The nineteen ninety-five-two thousand and one period.")]
    [InlineData("Version 2.0 shipped on 7 Oct. 2026, e.g. after the review.", "Version 2.0 shipped on 7 Oct. twenty twenty-six, e.g. after the review.")]
    public void RewritesYearsInsideText(string input, string expected)
    {
        Assert.Equal(expected, NumberSpeech.YearsAsSpeech(input));
    }

    [Theory]
    [InlineData("It costs £1995 today.")]
    [InlineData("About 1995.5 units.")]
    [InlineData("1,995 people came.")]
    [InlineData("Up 2026% since then.")]
    [InlineData("Model 12345 and 1234 are not years.")]
    [InlineData("Only 1400 left.")]
    public void LeavesOtherNumbersAlone(string input)
    {
        Assert.Equal(input, NumberSpeech.YearsAsSpeech(input));
    }

    [Fact]
    public void PipelineAppliesItUnlessSwitchedOff()
    {
        var on = new ReadingSettings();
        var off = new ReadingSettings { SayYearsNaturally = false };

        Assert.Equal("Back in nineteen ninety-five.", SpokenTextRules.ToSpeech("Back in 1995.", on));
        Assert.Equal("Back in 1995.", SpokenTextRules.ToSpeech("Back in 1995.", off));
    }
}
