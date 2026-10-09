using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Helpers.Core.Words;

public enum PartOfSpeech
{
    Noun,
    Verb,
    Adjective,
    Adverb,
}

/// <summary>One sense of a word: what it means, an example if there is one, and the other words for it.</summary>
public sealed record WordMeaning(PartOfSpeech Part, string Definition, IReadOnlyList<string> Examples, IReadOnlyList<string> Synonyms)
{
    public string PartText => Part switch
    {
        PartOfSpeech.Noun => "noun",
        PartOfSpeech.Verb => "verb",
        PartOfSpeech.Adjective => "adjective",
        PartOfSpeech.Adverb => "adverb",
        _ => string.Empty,
    };

    public string ExampleText => Examples.Count > 0 ? $"“{Examples[0]}”" : string.Empty;

    public string SynonymText => Synonyms.Count > 0 ? string.Join(", ", Synonyms) : string.Empty;
}

/// <summary>
/// Meanings and synonyms from Open English WordNet in its database form
/// (index.noun, data.noun and so on). The index files are read into memory
/// on first use; the data files are read at the offsets the index gives.
/// Simple plural and tense endings are tried, as WordNet's own tools do.
/// </summary>
public sealed class WordNet
{
    private static readonly (PartOfSpeech Part, string Suffix)[] Parts =
    [
        (PartOfSpeech.Noun, "noun"),
        (PartOfSpeech.Verb, "verb"),
        (PartOfSpeech.Adjective, "adj"),
        (PartOfSpeech.Adverb, "adv"),
    ];

    private readonly string _folder;
    private readonly object _sync = new();
    private Dictionary<PartOfSpeech, Dictionary<string, long[]>>? _index;
    private Dictionary<PartOfSpeech, Dictionary<string, string>>? _exceptions;

    public WordNet(string folder)
    {
        _folder = folder;
    }

    public string Folder => _folder;

    public bool IsAvailable => File.Exists(Path.Combine(_folder, "data.noun")) && File.Exists(Path.Combine(_folder, "index.noun"));

    /// <summary>Up to <paramref name="limit"/> senses, most common first, across the parts of speech.</summary>
    public IReadOnlyList<WordMeaning> Lookup(string word, int limit = 6)
    {
        if (string.IsNullOrWhiteSpace(word) || !IsAvailable)
        {
            return [];
        }

        var (index, exceptions) = Load();
        var key = word.Trim().ToLowerInvariant().Replace(' ', '_');
        var results = new List<WordMeaning>();

        foreach (var (part, suffix) in Parts)
        {
            var lemma = FindLemma(index[part], exceptions[part], key, part);
            if (lemma is null)
            {
                continue;
            }

            foreach (var offset in index[part][lemma])
            {
                if (results.Count >= limit)
                {
                    break;
                }

                var meaning = ReadSynset(part, suffix, offset, lemma);
                if (meaning is not null)
                {
                    results.Add(meaning);
                }
            }
        }

        return results;
    }

    /// <summary>The base form WordNet knows, trying the word as is, the exception list, then common endings.</summary>
    private static string? FindLemma(Dictionary<string, long[]> index, Dictionary<string, string> exceptions, string key, PartOfSpeech part)
    {
        if (index.ContainsKey(key))
        {
            return key;
        }

        if (exceptions.TryGetValue(key, out var exception) && index.ContainsKey(exception))
        {
            return exception;
        }

        foreach (var candidate in Morphy(key, part))
        {
            if (index.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> Morphy(string word, PartOfSpeech part)
    {
        var rules = part switch
        {
            PartOfSpeech.Noun => new[] { ("s", ""), ("ses", "s"), ("xes", "x"), ("zes", "z"), ("ches", "ch"), ("shes", "sh"), ("men", "man"), ("ies", "y") },
            PartOfSpeech.Verb => new[] { ("s", ""), ("ies", "y"), ("es", "e"), ("es", ""), ("ed", "e"), ("ed", ""), ("ing", "e"), ("ing", "") },
            PartOfSpeech.Adjective => new[] { ("er", ""), ("est", ""), ("er", "e"), ("est", "e") },
            _ => [],
        };

        foreach (var (ending, replacement) in rules)
        {
            if (word.EndsWith(ending, StringComparison.Ordinal) && word.Length > ending.Length + 1)
            {
                yield return word[..^ending.Length] + replacement;
            }
        }
    }

    private WordMeaning? ReadSynset(PartOfSpeech part, string suffix, long offset, string lemma)
    {
        var path = Path.Combine(_folder, "data." + suffix);
        string? line;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(offset, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, leaveOpen: true);
            line = reader.ReadLine();
        }
        catch (IOException)
        {
            return null;
        }

        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        // synset_offset lex_filenum ss_type w_cnt word lex_id [word lex_id...] p_cnt [ptr...] | gloss
        var bar = line.IndexOf('|');
        var head = bar >= 0 ? line[..bar] : line;
        var gloss = bar >= 0 ? line[(bar + 1)..].Trim() : string.Empty;
        var fields = head.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 4 || !int.TryParse(fields[3], System.Globalization.NumberStyles.HexNumber, null, out var wordCount))
        {
            return null;
        }

        var synonyms = new List<string>();
        for (var i = 0; i < wordCount && 4 + i * 2 < fields.Length; i++)
        {
            var raw = fields[4 + i * 2];
            var paren = raw.IndexOf('(');
            var text = (paren > 0 ? raw[..paren] : raw).Replace('_', ' ');
            if (!string.Equals(text, lemma.Replace('_', ' '), StringComparison.OrdinalIgnoreCase))
            {
                synonyms.Add(text);
            }
        }

        var (definition, examples) = SplitGloss(gloss);
        return new WordMeaning(part, definition, examples, synonyms);
    }

    /// <summary>The gloss is a definition, then examples in quotes separated by semicolons.</summary>
    private static (string Definition, IReadOnlyList<string> Examples) SplitGloss(string gloss)
    {
        var parts = gloss.Split(';');
        var definition = parts[0].Trim();
        var examples = new List<string>();
        for (var i = 1; i < parts.Length; i++)
        {
            var piece = parts[i].Trim().Trim('"').Trim();
            if (piece.Length > 0)
            {
                examples.Add(piece);
            }
        }

        if (definition.StartsWith('"'))
        {
            examples.Insert(0, definition.Trim('"'));
            definition = string.Empty;
        }

        return (definition, examples);
    }

    private (Dictionary<PartOfSpeech, Dictionary<string, long[]>> Index, Dictionary<PartOfSpeech, Dictionary<string, string>> Exceptions) Load()
    {
        lock (_sync)
        {
            if (_index is not null && _exceptions is not null)
            {
                return (_index, _exceptions);
            }

            var index = new Dictionary<PartOfSpeech, Dictionary<string, long[]>>();
            var exceptions = new Dictionary<PartOfSpeech, Dictionary<string, string>>();
            foreach (var (part, suffix) in Parts)
            {
                index[part] = ReadIndex(Path.Combine(_folder, "index." + suffix));
                exceptions[part] = ReadExceptions(Path.Combine(_folder, suffix + ".exc"));
            }

            _index = index;
            _exceptions = exceptions;
            return (index, exceptions);
        }
    }

    /// <summary>lemma pos synset_cnt p_cnt [ptr_symbol...] sense_cnt tagsense_cnt synset_offset [synset_offset...]</summary>
    private static Dictionary<string, long[]> ReadIndex(string path)
    {
        var entries = new Dictionary<string, long[]>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return entries;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0 || line[0] == ' ')
            {
                continue;
            }

            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 6 || !int.TryParse(fields[2], out var synsetCount) || !int.TryParse(fields[3], out var pointerCount))
            {
                continue;
            }

            var first = 4 + pointerCount + 2;
            var offsets = new List<long>(synsetCount);
            for (var i = first; i < fields.Length && offsets.Count < synsetCount; i++)
            {
                if (long.TryParse(fields[i], out var offset))
                {
                    offsets.Add(offset);
                }
            }

            entries[fields[0]] = offsets.ToArray();
        }

        return entries;
    }

    private static Dictionary<string, string> ReadExceptions(string path)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return entries;
        }

        foreach (var line in File.ReadLines(path))
        {
            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length >= 2)
            {
                entries.TryAdd(fields[0], fields[1]);
            }
        }

        return entries;
    }
}

/// <summary>Fetches Open English WordNet once, as its database zip from the project's GitHub release, checked and unpacked.</summary>
public static class WordNetDownload
{
    public const string Url = "https://github.com/globalwordnet/english-wordnet/releases/download/2025-edition/english-wordnet-2025.zip";
    public const string Sha256 = "73355e48f8117a24ca9ebc23ed75b35434e6cd21cc9dd3984e80aff5a5f63636";
    public const long Size = 9_618_697;

    public static string FolderUnder(string modelsRoot) => Path.Combine(modelsRoot, "wordnet");

    public static async Task DownloadAsync(string folder, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folder);
        var zip = Path.Combine(folder, "english-wordnet-2025.zip.part");

        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Helpers/0.1 (+https://github.com/drhumphrey/helpers)");

        try
        {
            using var response = await http.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? Size;

            using var sha = SHA256.Create();
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var target = new FileStream(zip, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
            {
                var buffer = new byte[1 << 16];
                long done = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    done += read;
                    progress?.Report(total > 0 ? (double)done / total : 0);
                }

                sha.TransformFinalBlock([], 0, 0);
            }

            var actual = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            if (!string.Equals(actual, Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The dictionary download didn't match its checksum, so it was discarded.");
            }

            // The zip holds one folder (oewn2025) with the database files; flatten it into the target folder.
            using (var archive = ZipFile.OpenRead(zip))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.Length == 0)
                    {
                        continue;
                    }

                    var destination = Path.Combine(folder, entry.Name);
                    entry.ExtractToFile(destination, overwrite: true);
                }
            }
        }
        finally
        {
            try
            {
                File.Delete(zip);
            }
            catch (IOException)
            {
            }
        }
    }
}
