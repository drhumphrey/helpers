using Helpers.Core.Settings;

namespace Helpers.Tests.Settings;

public sealed class DataFolderMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "HelpersTests", Guid.NewGuid().ToString("N"));

    public DataFolderMigrationTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void MovesTheOldFolderAcrossWithEverythingInIt()
    {
        var old = Path.Combine(_root, "Helpers");
        Directory.CreateDirectory(Path.Combine(old, "models", "wordnet"));
        File.WriteAllText(Path.Combine(old, "settings.json"), "{\"VoiceId\":\"bm_fable\"}");
        File.WriteAllText(Path.Combine(old, "models", "wordnet", "data.noun"), "words");

        var result = DataFolderMigration.Move(_root, "Helpers", "Unmuddle");

        Assert.Equal(DataFolderMigration.Result.Moved, result);
        Assert.False(Directory.Exists(old));
        Assert.Equal("{\"VoiceId\":\"bm_fable\"}", File.ReadAllText(Path.Combine(_root, "Unmuddle", "settings.json")));
        Assert.Equal("words", File.ReadAllText(Path.Combine(_root, "Unmuddle", "models", "wordnet", "data.noun")));
    }

    [Fact]
    public void DoesNothingWithoutAnOldFolder()
    {
        Assert.Equal(DataFolderMigration.Result.NothingToMove, DataFolderMigration.Move(_root, "Helpers", "Unmuddle"));
        Assert.False(Directory.Exists(Path.Combine(_root, "Unmuddle")));
    }

    [Fact]
    public void LeavesBothAloneWhenTheNewFolderIsThereAlready()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Helpers"));
        File.WriteAllText(Path.Combine(_root, "Helpers", "settings.json"), "old");
        Directory.CreateDirectory(Path.Combine(_root, "Unmuddle"));
        File.WriteAllText(Path.Combine(_root, "Unmuddle", "settings.json"), "new");

        Assert.Equal(DataFolderMigration.Result.AlreadyThere, DataFolderMigration.Move(_root, "Helpers", "Unmuddle"));
        Assert.Equal("old", File.ReadAllText(Path.Combine(_root, "Helpers", "settings.json")));
        Assert.Equal("new", File.ReadAllText(Path.Combine(_root, "Unmuddle", "settings.json")));
    }

    [Fact]
    public void ReportsAFailedMoveAndLeavesTheOldFolder()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Windows won't rename a folder while a file in it is open.
        Directory.CreateDirectory(Path.Combine(_root, "Helpers"));
        var file = Path.Combine(_root, "Helpers", "settings.json");
        File.WriteAllText(file, "kept");
        using (File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Equal(DataFolderMigration.Result.Failed, DataFolderMigration.Move(_root, "Helpers", "Unmuddle"));
        }

        Assert.Equal("kept", File.ReadAllText(file));
        Assert.False(Directory.Exists(Path.Combine(_root, "Unmuddle")));
    }
}
