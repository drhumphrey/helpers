namespace Helpers.Core.Settings;

/// <summary>
/// The first versions shipped as Helpers. On the first start as Unmuddle, their folders move
/// across, so settings, the draft, the user's own words and the downloads (about 3 GB) carry on.
/// A move, not a copy: both folders are under the same user profile, so it's instant.
/// </summary>
public static class DataFolderMigration
{
    public enum Result
    {
        /// <summary>There was no old folder.</summary>
        NothingToMove,

        /// <summary>The old folder now has the new name.</summary>
        Moved,

        /// <summary>The new folder exists already, so the old one was left alone.</summary>
        AlreadyThere,

        /// <summary>The move was refused, for instance because a file was open. Both are left as they were.</summary>
        Failed,
    }

    /// <summary>Renames root\previousName to root\currentName when only the old one exists. Never merges or overwrites.</summary>
    public static Result Move(string root, string previousName, string currentName)
    {
        var from = Path.Combine(root, previousName);
        var to = Path.Combine(root, currentName);
        if (!Directory.Exists(from))
        {
            return Result.NothingToMove;
        }

        if (Directory.Exists(to))
        {
            return Result.AlreadyThere;
        }

        try
        {
            Directory.Move(from, to);
            return Result.Moved;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed;
        }
    }

    /// <summary>Both of the app's folders: settings under the roaming profile, downloads under the local one.</summary>
    public static void MoveBoth()
    {
        Move(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), HelpersInfo.PreviousName, HelpersInfo.DataFolderName);
        Move(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), HelpersInfo.PreviousName, HelpersInfo.DataFolderName);
    }
}
