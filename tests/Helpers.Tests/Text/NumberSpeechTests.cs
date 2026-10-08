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
    [InlineData("Version 2.0 shipped on 7 Oct. 2026, e.g. after the review.", "Version 2.0 shipped on 7 Oct. twenty twenty-six, e.g. after the review.")]
    public void RewritesYearsInsideText(string input, string expected)
    {
        Assert.Equal(expected, NumberSpeech.YearsAsSpeech(input));
    }

    [Theory]
    [InlineData("About 1995.5 units.")]
    [InlineData("1,995 people came.")]
    [InlineData("Model 12345 and 1234 are not years.")]
    [InlineData("Only 1400 left.")]
    public void LeavesOtherFourDigitNumbersAlone(string input)
    {
        Assert.Equal(input, NumberSpeech.YearsAsSpeech(input));
    }

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(7, "seven")]
    [InlineData(21, "twenty-one")]
    [InlineData(100, "one hundred")]
    [InlineData(895, "eight hundred and ninety-five")]
    [InlineData(1250, "one thousand two hundred and fifty")]
    [InlineData(1005, "one thousand and five")]
    [InlineData(2_000_000, "two million")]
    [InlineData(3_500_020, "three million five hundred thousand and twenty")]
    public void SaysWholeNumbersInBritishWords(long number, string expected)
    {
        Assert.Equal(expected, NumberSpeech.SayNumber(number));
    }

    [Theory]
    [InlineData("It costs £895.00 today.", "It costs eight hundred and ninety-five pounds today.")]
    [InlineData("It costs £895.50 today.", "It costs eight hundred and ninety-five pounds fifty today.")]
    [InlineData("Only £0.99!", "Only ninety-nine pence!")]
    [InlineData("Only £0.01!", "Only one penny!")]
    [InlineData("Just £1.", "Just one pound.")]
    [InlineData("Budget £1,250,000 total.", "Budget one million two hundred and fifty thousand pounds total.")]
    [InlineData("Raised £1.2m last year.", "Raised one point two million pounds last year.")]
    [InlineData("Worth $3bn now.", "Worth three billion dollars now.")]
    [InlineData("About $2.50 each.", "About two dollars fifty each.")]
    [InlineData("Fee €45.", "Fee forty-five euros.")]
    [InlineData("£1995 was the price.", "one thousand nine hundred and ninety-five pounds was the price.")]
    public void SaysMoneyTheWayPeopleDo(string input, string expected)
    {
        Assert.Equal(expected, NumberSpeech.MoneyAsSpeech(input));
    }

    [Theory]
    [InlineData("Up 25% this year.", "Up twenty-five per cent this year.")]
    [InlineData("Up 2.5 % this year.", "Up two point five per cent this year.")]
    [InlineData("A 100% pass rate.", "A one hundred per cent pass rate.")]
    public void SaysPercentages(string input, string expected)
    {
        Assert.Equal(expected, NumberSpeech.PercentAsSpeech(input));
    }

    [Theory]
    [InlineData("Meet at 10:30.", "Meet at ten thirty.")]
    [InlineData("Meet at 10:05.", "Meet at ten oh five.")]
    [InlineData("Meet at 9:00.", "Meet at nine o'clock.")]
    [InlineData("Meet at 9:00 am.", "Meet at nine a m.")]
    [InlineData("Meet at 14:30.", "Meet at fourteen thirty.")]
    [InlineData("Meet at 2:15pm.", "Meet at two fifteen p m.")]
    public void SaysClockTimes(string input, string expected)
    {
        Assert.Equal(expected, NumberSpeech.TimesAsSpeech(input));
    }

    [Fact]
    public void LeavesTimestampsWithSecondsAlone()
    {
        Assert.Equal("At 10:30:15 it failed.", NumberSpeech.TimesAsSpeech("At 10:30:15 it failed."));
    }

    [Theory]
    [InlineData("The 1st of May.", "The first of May.")]
    [InlineData("The 2nd and 3rd.", "The second and third.")]
    [InlineData("On the 21st.", "On the twenty-first.")]
    [InlineData("Its 100th run.", "Its one hundredth run.")]
    [InlineData("The 12th.", "The twelfth.")]
    public void SaysOrdinals(string input, string expected)
    {
        Assert.Equal(expected, NumberSpeech.OrdinalsAsSpeech(input));
    }

    [Fact]
    public void ApplyRunsEverythingTogether()
    {
        var input = "On the 1st of Oct. 2026 at 10:30 we paid £895.00, 25% more than in 1995.";

        var result = NumberSpeech.Apply(input);

        Assert.Equal(
            "On the first of Oct. twenty twenty-six at ten thirty we paid eight hundred and ninety-five pounds, twenty-five per cent more than in nineteen ninety-five.",
            result);
    }

    [Fact]
    public void PipelineAppliesItUnlessSwitchedOff()
    {
        var on = new ReadingSettings();
        var off = new ReadingSettings { SayNumbersNaturally = false };

        Assert.Equal("Back in nineteen ninety-five.", SpokenTextRules.ToSpeech("Back in 1995.", on));
        Assert.Equal("Back in 1995.", SpokenTextRules.ToSpeech("Back in 1995.", off));
    }
}
