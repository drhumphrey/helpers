using Helpers.Core.Updates;

namespace Helpers.Tests.Updates;

public class UpdateCheckTests
{
    [Fact]
    public void ParsesTheVersionFile()
    {
        var json =
            """
            {
              "version": "0.2.0",
              "notes": "Compose, the AI helper and the word tools.",
              "page": "https://github.com/drhumphrey/unmuddle/releases/tag/v0.2.0",
              "installer": { "url": "https://github.com/drhumphrey/unmuddle/releases/download/v0.2.0/Unmuddle-0.2.0-setup.exe", "sha256": "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789" },
              "zip": { "url": "https://github.com/drhumphrey/unmuddle/releases/download/v0.2.0/Unmuddle-0.2.0-win-x64.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" }
            }
            """;

        var info = UpdateCheck.Parse(json);

        Assert.NotNull(info);
        Assert.Equal("0.2.0", info.Version);
        Assert.Equal("Compose, the AI helper and the word tools.", info.Notes);
        Assert.NotNull(info.Installer);
        Assert.Equal("abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789", info.Installer.Sha256);
        Assert.NotNull(info.Zip);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"notes\":\"no version\"}")]
    [InlineData("{\"version\":\"latest\"}")]
    public void RejectsAnythingThatIsNotAVersionFile(string json)
    {
        Assert.Null(UpdateCheck.Parse(json));
    }

    [Fact]
    public void AFileWithABadUrlOrChecksumIsDropped()
    {
        var json = """{"version":"0.2.0","installer":{"url":"http://example.com/setup.exe","sha256":"abc"}}""";

        var info = UpdateCheck.Parse(json);

        Assert.NotNull(info);
        Assert.Null(info.Installer);
    }

    [Theory]
    [InlineData("0.2.0", "0.1.0", true)]
    [InlineData("v0.2.0", "0.1.0", true)]
    [InlineData("0.1.1", "0.1.0", true)]
    [InlineData("1.0", "0.9.9", true)]
    [InlineData("0.1.0", "0.1.0", false)]
    [InlineData("0.1.0", "0.2.0", false)]
    [InlineData("0.2.0-preview", "0.1.0", true)]
    [InlineData("rubbish", "0.1.0", false)]
    public void ComparesVersionsNumerically(string candidate, string current, bool newer)
    {
        Assert.Equal(newer, UpdateCheck.IsNewer(candidate, current));
    }

    [Fact]
    public void ChecksAtMostOnceADay()
    {
        var now = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

        Assert.True(UpdateCheck.IsDue(null, now));
        Assert.False(UpdateCheck.IsDue(now.AddHours(-2), now));
        Assert.True(UpdateCheck.IsDue(now.AddHours(-23), now));
        Assert.True(UpdateCheck.IsDue(now.AddDays(-3), now));
    }

    [Fact]
    public void HandsOverToTheInstallerWithTheWholeLineQuotedForCmd()
    {
        var arguments = UpdateCheck.HandOverArguments(
            @"C:\Users\Jo Bloggs\AppData\Local\Temp\Unmuddle-0.2.0-setup.exe",
            @"C:\Users\Jo Bloggs\AppData\Local\Programs\Unmuddle\Unmuddle.exe");

        Assert.Equal(
            "/s /c \"\"C:\\Users\\Jo Bloggs\\AppData\\Local\\Temp\\Unmuddle-0.2.0-setup.exe\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS"
            + " & start \"\" \"C:\\Users\\Jo Bloggs\\AppData\\Local\\Programs\\Unmuddle\\Unmuddle.exe\"\"",
            arguments);
    }

    [Fact]
    public void HandsOverWithoutARestartWhenTheAppPathIsUnknown()
    {
        var arguments = UpdateCheck.HandOverArguments(@"C:\Temp\Unmuddle-0.2.0-setup.exe", null);

        Assert.Equal("/s /c \"\"C:\\Temp\\Unmuddle-0.2.0-setup.exe\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS\"", arguments);
    }

    [Theory]
    [InlineData(@"C:\Temp\100%\Unmuddle-0.2.0-setup.exe", @"C:\Apps\Helpers.App.exe")]
    [InlineData(@"C:\Temp\Unmuddle-0.2.0-setup.exe", @"C:\Users\%USERNAME%\Helpers.App.exe")]
    [InlineData("C:\\Temp\\odd\"name.exe", @"C:\Apps\Helpers.App.exe")]
    [InlineData("", @"C:\Apps\Helpers.App.exe")]
    public void RefusesPathsCmdCantBeTrustedWith(string installer, string app)
    {
        Assert.Null(UpdateCheck.HandOverArguments(installer, app));
    }
}
