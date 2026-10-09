using Helpers.Windows.Spelling;

namespace Helpers.Windows.Tests;

/// <summary>
/// Talks to the real Windows spell checker. Each test is a no-op on a machine
/// without the English (United Kingdom) language features, so CI never fails
/// for a missing language pack.
/// </summary>
public class WindowsSpellCheckerTests
{
    [Fact]
    public void FindsAMisspeltWordAndSuggestsTheRightOne()
    {
        var checker = new WindowsSpellChecker("en-GB");
        if (!checker.IsAvailable)
        {
            return;
        }

        var errors = checker.Check("Please recieve the parcel tomorow.");

        Assert.Equal(2, errors.Count);
        Assert.Equal(7, errors[0].Start);
        Assert.Equal(7, errors[0].Length);
        Assert.Contains("receive", checker.Suggest("recieve"));
        Assert.Contains("tomorrow", checker.Suggest("tomorow"));
    }

    [Fact]
    public void CorrectTextHasNoErrors()
    {
        var checker = new WindowsSpellChecker("en-GB");
        if (!checker.IsAvailable)
        {
            return;
        }

        Assert.Empty(checker.Check("The client wants the registration form to remember what they typed."));
    }

    [Fact]
    public void IgnoredWordsStopBeingErrors()
    {
        var checker = new WindowsSpellChecker("en-GB");
        if (!checker.IsAvailable)
        {
            return;
        }

        Assert.Single(checker.Check("Open the slnx file."));
        checker.Ignore("slnx");
        Assert.Empty(checker.Check("Open the slnx file."));
    }
}
