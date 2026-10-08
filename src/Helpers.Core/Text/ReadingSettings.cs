namespace Helpers.Core.Text;

/// <summary>How file paths such as <c>src/Helpers.Core/Splitter.cs</c> are read out.</summary>
public enum FilePathReading
{
    /// <summary>Read the whole path as written.</summary>
    Full,

    /// <summary>Say "file" followed by the file name only.</summary>
    FileNameOnly,

    /// <summary>Just say "file".</summary>
    JustSayFile,
}

/// <summary>How fenced code blocks are read out.</summary>
public enum CodeBlockReading
{
    /// <summary>Say "code block, 12 lines" and skip the contents.</summary>
    Skip,

    /// <summary>Say the count, then read the first line.</summary>
    FirstLine,

    /// <summary>Read every line.</summary>
    All,
}

/// <summary>
/// The user's choices for how text is turned into speech. Lives in settings.json.
/// </summary>
public sealed class ReadingSettings
{
    /// <summary>Say "link" instead of reading a web address out in full.</summary>
    public bool SayLinkForUrls { get; set; } = true;

    /// <summary>Say "email address" instead of reading an address out.</summary>
    public bool SayEmailAddress { get; set; } = true;

    public FilePathReading FilePaths { get; set; } = FilePathReading.FileNameOnly;

    public CodeBlockReading CodeBlocks { get; set; } = CodeBlockReading.Skip;

    /// <summary>Say only "table, 3 columns, 5 rows" and skip the rows.</summary>
    public bool SkipTables { get; set; }

    /// <summary>The longest piece of text handed to the engine in one go.</summary>
    public int MaxChunkLength { get; set; } = 400;

    public PronunciationDictionary Pronunciations { get; set; } = PronunciationDictionary.CreateStarter();
}
