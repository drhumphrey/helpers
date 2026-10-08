// ReadingDump: shows what the reading pipeline would say for a file.
//
// Usage:
//   dotnet run --project tools/ReadingDump -- <file> [--code first|all] [--skip-tables] [--paths full|name|file]
//
// Prints one line per segment: the kind, the pause after it, and the spoken text.
// Where the spoken text differs from the displayed text, the displayed text follows in brackets.

using Helpers.Core.Text;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: ReadingDump <file> [--code first|all] [--skip-tables] [--paths full|name|file]");
    return 1;
}

var settings = new ReadingSettings();
for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--code" when i + 1 < args.Length:
            settings.CodeBlocks = args[++i] switch
            {
                "first" => CodeBlockReading.FirstLine,
                "all" => CodeBlockReading.All,
                _ => CodeBlockReading.Skip,
            };
            break;
        case "--skip-tables":
            settings.SkipTables = true;
            break;
        case "--paths" when i + 1 < args.Length:
            settings.FilePaths = args[++i] switch
            {
                "full" => FilePathReading.Full,
                "file" => FilePathReading.JustSayFile,
                _ => FilePathReading.FileNameOnly,
            };
            break;
        default:
            Console.Error.WriteLine($"Unknown option {args[i]}.");
            return 1;
    }
}

var text = await File.ReadAllTextAsync(args[0]);
var markdown = MarkdownDetector.LooksLikeMarkdown(text);
var segments = ReadingPipeline.Prepare(text, settings);

Console.WriteLine($"{(markdown ? "Markdown" : "Plain text")}, {text.Length} characters, {segments.Count} segments");
Console.WriteLine();

foreach (var segment in segments)
{
    var kind = segment.Kind.ToString().ToLowerInvariant().PadRight(12);
    var pause = $"{segment.PauseAfterMs} ms".PadLeft(7);
    Console.Write($"{kind} {pause}  {segment.Speak}");
    if (segment.Display != segment.Speak)
    {
        Console.Write($"   [{segment.Display}]");
    }

    Console.WriteLine();
}

return 0;
