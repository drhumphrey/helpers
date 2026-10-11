using Helpers.Core;

namespace Helpers.Tests;

public class HelpersInfoTests
{
    [Fact]
    public void Name_IsUnmuddle()
    {
        Assert.Equal("Unmuddle", HelpersInfo.Name);
    }

    [Fact]
    public void DataFolderName_HasNoPathSeparators()
    {
        Assert.DoesNotContain('\\', HelpersInfo.DataFolderName);
        Assert.DoesNotContain('/', HelpersInfo.DataFolderName);
    }

    [Fact]
    public void PreviousName_DiffersFromTheCurrentFolder()
    {
        Assert.NotEqual(HelpersInfo.DataFolderName, HelpersInfo.PreviousName, StringComparer.OrdinalIgnoreCase);
    }
}
