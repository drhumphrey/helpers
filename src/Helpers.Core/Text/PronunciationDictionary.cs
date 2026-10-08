using System.Text.RegularExpressions;

namespace Helpers.Core.Text;

/// <summary>One word and how to say it.</summary>
public sealed record PronunciationEntry(string Word, string SayAs, bool CaseSensitive = false);

/// <summary>
/// User-editable list of words the engine gets wrong, with whole-word matching.
/// Applied to the spoken text only, never to what is shown on screen.
/// </summary>
public sealed class PronunciationDictionary
{
    private readonly List<PronunciationEntry> _entries = [];
    private readonly List<(Regex Pattern, string SayAs)> _compiled = [];

    public IReadOnlyList<PronunciationEntry> Entries => _entries;

    /// <summary>Adds a word, replacing any existing entry for the same word.</summary>
    public void Add(string word, string sayAs, bool caseSensitive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(word);
        ArgumentNullException.ThrowIfNull(sayAs);

        Remove(word);
        _entries.Add(new PronunciationEntry(word.Trim(), sayAs.Trim(), caseSensitive));
        Recompile();
    }

    /// <summary>Removes a word. Returns false if it wasn't there.</summary>
    public bool Remove(string word)
    {
        var removed = _entries.RemoveAll(e => string.Equals(e.Word, word.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed)
        {
            Recompile();
        }

        return removed;
    }

    /// <summary>Replaces every whole-word match in the text with its spoken form.</summary>
    public string Apply(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        foreach (var (pattern, sayAs) in _compiled)
        {
            text = pattern.Replace(text, sayAs);
        }

        return text;
    }

    /// <summary>Words that AI chats use a lot and that voices tend to mangle.</summary>
    public static PronunciationDictionary CreateStarter()
    {
        var dictionary = new PronunciationDictionary();
        dictionary.Add("npm", "N P M");
        dictionary.Add("JSON", "jason");
        dictionary.Add("async", "a sink");
        dictionary.Add("regex", "rej ex");
        dictionary.Add("UIA", "U I A");
        dictionary.Add("GGUF", "G G U F");
        dictionary.Add("NuGet", "new get");
        dictionary.Add("csproj", "C S proj");
        dictionary.Add("slnx", "S L N X");
        dictionary.Add("EVAR", "ee-var");
        return dictionary;
    }

    private void Recompile()
    {
        _compiled.Clear();
        foreach (var entry in _entries)
        {
            // Not \b: words like ".NET" or "C#" have no word boundary at the symbol end.
            var pattern = @"(?<![\p{L}\p{N}_])" + Regex.Escape(entry.Word) + @"(?![\p{L}\p{N}_])";
            var options = RegexOptions.CultureInvariant | (entry.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            _compiled.Add((new Regex(pattern, options), entry.SayAs));
        }
    }
}
