namespace Helpers.Core;

/// <summary>
/// Basic facts about the application that every project can rely on. The code keeps its
/// working title, Helpers, for projects and namespaces; everything a user sees says Unmuddle.
/// </summary>
public static class HelpersInfo
{
    /// <summary>The product name, shown in the UI.</summary>
    public const string Name = "Unmuddle";

    /// <summary>What it's for, in a line.</summary>
    public const string Tagline = "Making sense of what you read and write";

    /// <summary>The folder name used under the user's application data folders.</summary>
    public const string DataFolderName = "Unmuddle";

    /// <summary>The working title the first versions shipped under. Their folders, key and start-up entry move across on first start.</summary>
    public const string PreviousName = "Helpers";

    /// <summary>Where the source code, the releases and the issues live.</summary>
    public const string RepositoryUrl = "https://github.com/drhumphrey/unmuddle";
}
