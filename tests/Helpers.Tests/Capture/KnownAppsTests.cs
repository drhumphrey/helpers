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

    [Theory]
    [InlineData("ScreenClippingHost")]
    [InlineData("SnippingTool.exe")]
    [InlineData("screensketch")]
    public void KnowsWindowsScreenCaptureTools(string process)
    {
        Assert.True(KnownApps.IsScreenCaptureTool(process));
    }

    [Theory]
    [InlineData("LockApp")]
    [InlineData("SearchHost.exe")]
    [InlineData("startmenuexperiencehost")]
    public void KnowsTheWindowsShellIsNeverATarget(string process)
    {
        Assert.True(KnownApps.IsSystemShell(process));
    }

    [Theory]
    [InlineData("Code")]
    [InlineData("chrome")]
    [InlineData("explorer")]
    [InlineData(null)]
    public void OrdinaryAppsAreNotTheShell(string? process)
    {
        Assert.False(KnownApps.IsSystemShell(process));
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherAppsAreNotScreenCaptureTools(string? process)
    {
        Assert.False(KnownApps.IsScreenCaptureTool(process));
    }
}
