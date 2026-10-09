namespace Helpers.Core.Spelling;

/// <summary>
/// The user's own words, one per line in a plain text file they can read and
/// edit. Kept separate from the operating system's dictionary so it travels
/// with the app's settings and works the same on every platform.
/// </summary>
public sealed class UserDictionary
{
    private readonly HashSet<string> _words = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public UserDictionary(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _words.Count;
            }
        }
    }

    public IReadOnlyCollection<string> Words
    {
        get
        {
            lock (_sync)
            {
                return _words.ToArray();
            }
        }
    }

    /// <summary>Reads the file. A missing or unreadable file gives an empty dictionary, never an exception.</summary>
    public void Load()
    {
        lock (_sync)
        {
            _words.Clear();
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (var line in File.ReadLines(FilePath))
                    {
                        var word = line.Trim();
                        if (word.Length > 0)
                        {
                            _words.Add(word);
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public bool Contains(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        lock (_sync)
        {
            return _words.Contains(word.Trim());
        }
    }

    /// <summary>Adds a word and appends it to the file. Returns false if it was already there.</summary>
    public bool Add(string word)
    {
        var trimmed = word.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        lock (_sync)
        {
            if (!_words.Add(trimmed))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllLines(FilePath, [trimmed]);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return true;
        }
    }
}
