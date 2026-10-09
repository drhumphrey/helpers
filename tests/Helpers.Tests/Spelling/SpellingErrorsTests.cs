using Helpers.Core.Spelling;

namespace Helpers.Tests.Spelling;

public class SpellingErrorsTests
{
    // "Please recieve the parcel tomorow."
    //  0123456789012345678901234567890123
    private static readonly SpellingError Recieve = new(7, 7);
    private static readonly SpellingError Tomorow = new(26, 7);

    [Fact]
    public void HidesTheWordTheCaretIsIn()
    {
        var kept = SpellingErrors.HideWordBeingTyped([Recieve, Tomorow], 10);

        Assert.Equal([Tomorow], kept);
    }

    [Fact]
    public void HidesTheWordTheCaretHasJustFinished()
    {
        var kept = SpellingErrors.HideWordBeingTyped([Recieve, Tomorow], 33);

        Assert.Equal([Recieve], kept);
    }

    [Fact]
    public void KeepsEverythingWhenTheCaretIsElsewhere()
    {
        var errors = new[] { Recieve, Tomorow };

        Assert.Same(errors, SpellingErrors.HideWordBeingTyped(errors, 20));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(10)]
    [InlineData(14)]
    public void FindsTheErrorAtAnIndexInsideOrJustAfterIt(int index)
    {
        Assert.Equal(Recieve, SpellingErrors.FindAt([Recieve, Tomorow], index));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(15)]
    public void FindsNothingOutsideAnError(int index)
    {
        Assert.Null(SpellingErrors.FindAt([Recieve, Tomorow], index));
    }

    // "Please recieve the parcel tomorow. I"
    //  0123456789012345678901234567890123456
    [Theory]
    [InlineData(7, 7, 7)]
    [InlineData(10, 7, 7)]
    [InlineData(14, 7, 7)]
    [InlineData(0, 0, 6)]
    [InlineData(6, 0, 6)]
    [InlineData(35, 35, 1)]
    [InlineData(36, 35, 1)]
    public void FindsTheWordAroundAnIndexOrJustAfterIt(int index, int start, int length)
    {
        var text = "Please recieve the parcel tomorow. I";

        Assert.Equal(new SpellingError(start, length), SpellingErrors.WordAt(text, index));
    }

    [Theory]
    [InlineData("Please recieve the parcel tomorow. I", 34)]
    [InlineData(" leading", 0)]
    [InlineData("two  spaces", 4)]
    [InlineData("", 0)]
    public void NoWordWhenNothingTouchesTheIndex(string text, int index)
    {
        Assert.Null(SpellingErrors.WordAt(text, index));
    }

    [Fact]
    public void WordsKeepTheirApostrophesAndHyphens()
    {
        Assert.Equal(new SpellingError(0, 5), SpellingErrors.WordAt("don't go", 2));
        Assert.Equal(new SpellingError(4, 10), SpellingErrors.WordAt("the well-known one", 8));
    }

    [Fact]
    public void ShiftsLaterErrorsWhenTextIsInsertedBeforeThem()
    {
        var oldText = "Please recieve the parcel tomorow.";
        var newText = "Please recieve the big parcel tomorow.";

        var shifted = SpellingErrors.Shift([Recieve, Tomorow], oldText, newText);

        Assert.Equal([Recieve, new SpellingError(30, 7)], shifted);
    }

    [Fact]
    public void ShiftsLaterErrorsBackWhenTextIsDeleted()
    {
        var oldText = "Please recieve the parcel tomorow.";
        var newText = "Please recieve parcel tomorow.";

        var shifted = SpellingErrors.Shift([Recieve, Tomorow], oldText, newText);

        Assert.Equal([Recieve, new SpellingError(22, 7)], shifted);
    }

    [Fact]
    public void DropsAnErrorThatTheEditTouched()
    {
        var oldText = "Please recieve the parcel tomorow.";
        var newText = "Please receive the parcel tomorow.";

        var shifted = SpellingErrors.Shift([Recieve, Tomorow], oldText, newText);

        Assert.Equal([Tomorow], shifted);
    }

    [Fact]
    public void TypingAtTheEndLeavesEarlierErrorsAlone()
    {
        var oldText = "Please recieve the parcel tomorow.";
        var newText = "Please recieve the parcel tomorow. T";

        var shifted = SpellingErrors.Shift([Recieve, Tomorow], oldText, newText);

        Assert.Equal([Recieve, Tomorow], shifted);
    }

    [Fact]
    public void UnchangedTextReturnsTheSameList()
    {
        var errors = new[] { Recieve };

        Assert.Same(errors, SpellingErrors.Shift(errors, "same", "same"));
    }
}
