using Helpers.Core.Capture;

namespace Helpers.Tests.Capture;

public class KnownAppsTests
{
    [Theory]
    [InlineData("WindowsTerminal")]
    [InlineData("windowsterminal.exe")]
    [InlineData("Code")]
    [InlineData("pwsh")]
    public void KnowsTerminalsAndEditorsWithTerminals(string process)
    {
        Assert.True(KnownApps.IsTerminal(process));
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("OUTLOOK")]
    [InlineData("")]
    [InlineData(null)]
    public void EverythingElseIsNotATerminal(string? process)
    {
        Assert.False(KnownApps.IsTerminal(process));
    }
}
