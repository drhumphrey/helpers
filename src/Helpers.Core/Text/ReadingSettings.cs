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

    /// <summary>
    /// Say money, percentages, clock times, ordinals and years the way people do:
    /// "eight hundred and ninety-five pounds", "nineteen ninety-five", "twenty-first".
    /// </summary>
    public bool SayNumbersNaturally { get; set; } = true;

    /// <summary>The longest piece of text handed to the engine in one go.</summary>
    public int MaxChunkLength { get; set; } = 400;

    [System.Text.Json.Serialization.JsonIgnore]
    public PronunciationDictionary Pronunciations { get; set; } = PronunciationDictionary.CreateStarter();

    /// <summary>The dictionary's entries in a shape the settings file can hold.</summary>
    public List<PronunciationEntry> PronunciationEntries
    {
        get => [.. Pronunciations.Entries];
        set
        {
            var dictionary = new PronunciationDictionary();
            foreach (var entry in value ?? [])
            {
                dictionary.Add(entry.Word, entry.SayAs, entry.CaseSensitive);
            }

            Pronunciations = dictionary;
        }
    }
}
