using Helpers.Core.Text;

namespace Helpers.Tests.Text;

public class SpokenTextRulesTests
{
    [Theory]
    [InlineData("See https://github.com/drhumphrey/helpers for the code.", "See link for the code.")]
    [InlineData("Go to www.example.org.", "Go to link.")]
    [InlineData("Two: http://a.com/x?y=1, and https://b.org/path.", "Two: link, and link.")]
    public void WebAddressesBecomeLink(string input, string expected)
    {
        Assert.Equal(expected, SpokenTextRules.ReplaceUrls(input));
    }

    [Fact]
    public void EmailAddressesBecomeEmailAddress()
    {
        Assert.Equal("Write to email address today.", SpokenTextRules.ReplaceEmails("Write to david@example.com today."));
    }

    [Theory]
    [InlineData("Open src/Helpers.Core/Splitter.cs now.", FilePathReading.FileNameOnly, "Open file Splitter.cs now.")]
    [InlineData("Open src/Helpers.Core/Splitter.cs now.", FilePathReading.JustSayFile, "Open file now.")]
    [InlineData("Open src/Helpers.Core/Splitter.cs now.", FilePathReading.Full, "Open src/Helpers.Core/Splitter.cs now.")]
    [InlineData("Saved to C:\\Users\\david\\notes.txt.", FilePathReading.FileNameOnly, "Saved to file notes.txt.")]
    [InlineData("Edit docs/BRIEF.md please.", FilePathReading.FileNameOnly, "Edit file BRIEF.md please.")]
    public void FilePathsFollowTheSetting(string input, FilePathReading mode, string expected)
    {
        Assert.Equal(expected, SpokenTextRules.ReplaceFilePaths(input, mode));
    }

    [Theory]
    [InlineData("Use and/or as needed.")]
    [InlineData("Due on 8/10/2026 at noon.")]
    [InlineData("About 3/4 of them.")]
    public void OrdinaryTextWithSlashesIsLeftAlone(string input)
    {
        Assert.Equal(input, SpokenTextRules.ReplaceFilePaths(input, FilePathReading.FileNameOnly));
    }

    [Fact]
    public void ToSpeechAppliesEverythingAndTidiesWhitespace()
    {
        var settings = new ReadingSettings();
        settings.Pronunciations.Add("Helpers", "helpers app");

        var result = SpokenTextRules.ToSpeech("  Helpers lives at https://x.com/y,   see src/a/b.cs  ", settings);

        Assert.Equal("helpers app lives at link, see file b.cs", result);
    }

    [Fact]
    public void SettingsCanTurnRulesOff()
    {
        var settings = new ReadingSettings { SayLinkForUrls = false, SayEmailAddress = false, FilePaths = FilePathReading.Full };

        var result = SpokenTextRules.ToSpeech("https://x.com and a@b.co and src/x/y.cs", settings);

        Assert.Equal("https://x.com and a@b.co and src/x/y.cs", result);
    }
}
