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
              "page": "https://github.com/drhumphrey/helpers/releases/tag/v0.2.0",
              "installer": { "url": "https://github.com/drhumphrey/helpers/releases/download/v0.2.0/Helpers-0.2.0-setup.exe", "sha256": "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789" },
              "zip": { "url": "https://github.com/drhumphrey/helpers/releases/download/v0.2.0/Helpers-0.2.0-win-x64.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" }
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
}
