using Helpers.Core;

namespace Helpers.Tests;

public class HelpersInfoTests
{
    [Fact]
    public void Name_IsTheWorkingTitle()
    {
        Assert.Equal("Helpers", HelpersInfo.Name);
    }

    [Fact]
    public void DataFolderName_HasNoPathSeparators()
    {
        Assert.DoesNotContain('\\', HelpersInfo.DataFolderName);
        Assert.DoesNotContain('/', HelpersInfo.DataFolderName);
    }
}
