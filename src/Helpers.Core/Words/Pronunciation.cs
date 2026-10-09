using System.Text;

namespace Helpers.Core.Words;

/// <summary>
/// How a word is said, from the British pronunciation list that ships with
/// the voice (misaki's lexicon, one word per line, phonemes separated by
/// spaces). Gives the IPA and a plain respelling with the stressed syllable
/// in capitals: rej-i-STRAY-shun.
/// </summary>
public sealed class Pronunciation
{
    private readonly string _lexiconPath;
    private Dictionary<string, string>? _entries;
    private readonly object _sync = new();

    public Pronunciation(string lexiconPath)
    {
        _lexiconPath = lexiconPath;
    }

    public bool IsAvailable => File.Exists(_lexiconPath);

    /// <summary>The raw phoneme string from the list, or null when the word isn't in it.</summary>
    public string? Phonemes(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || !IsAvailable)
        {
            return null;
        }

        var entries = Load();
        var key = word.Trim().ToLowerInvariant();
        return entries.TryGetValue(key, out var phonemes) ? phonemes : null;
    }

    public string? Ipa(string word) => Phonemes(word) is { } phonemes ? ToIpa(phonemes) : null;

    public string? Respelling(string word) => Phonemes(word) is { } phonemes ? Respell(phonemes) : null;

    /// <summary>Expands misaki's one-letter vowels to IPA and drops the spaces: /ɹˌɛdʒɪstɹˈeɪʃən/.</summary>
    public static string ToIpa(string phonemes)
    {
        var builder = new StringBuilder();
        foreach (var symbol in phonemes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            builder.Append(symbol switch
            {
                "A" => "eɪ",
                "I" => "aɪ",
                "W" => "aʊ",
                "Y" => "ɔɪ",
                "O" => "oʊ",
                "Q" => "əʊ",
                "ʤ" => "dʒ",
                "ʧ" => "tʃ",
                "ᵊ" => "ə",
                _ => symbol,
            });
        }

        return builder.ToString();
    }

    /// <summary>
    /// A respelling anyone can read: each sound as the letters it usually has in
    /// English, syllables joined with hyphens, the stressed one in capitals.
    /// </summary>
    public static string Respell(string phonemes)
    {
        var symbols = phonemes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Group into syllables: a vowel with the consonants before it. A cluster of two or more
        // consonants between vowels leaves its first consonant with the earlier syllable.
        var syllables = new List<(StringBuilder Text, bool Stressed)>();
        var pending = new List<string>();
        var pendingStress = false;

        for (var i = 0; i < symbols.Length; i++)
        {
            var symbol = symbols[i];
            if (symbol is "ˈ" or "ˌ")
            {
                pendingStress = symbol == "ˈ";
                continue;
            }

            if (symbol == "ː")
            {
                if (syllables.Count > 0)
                {
                    LengthenLast(syllables[^1].Text);
                }

                continue;
            }

            if (!IsVowel(symbol))
            {
                pending.Add(symbol);
                continue;
            }

            if (syllables.Count > 0 && pending.Count >= 2)
            {
                syllables[^1].Text.Append(Letters(pending[0]));
                pending.RemoveAt(0);
            }

            var text = new StringBuilder();
            foreach (var consonant in pending)
            {
                text.Append(Letters(consonant));
            }

            pending.Clear();
            text.Append(Letters(symbol));
            syllables.Add((text, pendingStress));
            pendingStress = false;
        }

        if (pending.Count > 0)
        {
            var tail = new StringBuilder();
            foreach (var consonant in pending)
            {
                tail.Append(Letters(consonant));
            }

            if (syllables.Count > 0)
            {
                syllables[^1].Text.Append(tail);
            }
            else
            {
                syllables.Add((tail, false));
            }
        }

        return string.Join("-", syllables.Select(s => s.Stressed ? s.Text.ToString().ToUpperInvariant() : s.Text.ToString()));
    }

    private static void LengthenLast(StringBuilder text)
    {
        // The long mark follows a vowel that was already written in its long form (see Letters), so nothing to add.
    }

    private static bool IsVowel(string symbol) => symbol switch
    {
        "A" or "I" or "W" or "Y" or "O" or "Q" => true,
        "ɪ" or "i" or "ɛ" or "a" or "ɑ" or "ɒ" or "ɔ" or "ʊ" or "u" or "ʌ" or "ə" or "ᵊ" or "ɜ" or "e" or "o" or "æ" => true,
        _ => false,
    };

    private static string Letters(string symbol) => symbol switch
    {
        "A" => "ay",
        "I" => "eye",
        "W" => "ow",
        "Y" => "oy",
        "O" => "oh",
        "Q" => "oh",
        "ɪ" => "i",
        "i" => "ee",
        "ɛ" => "e",
        "a" => "a",
        "æ" => "a",
        "ɑ" => "ah",
        "ɒ" => "o",
        "ɔ" => "aw",
        "ʊ" => "uu",
        "u" => "oo",
        "ʌ" => "u",
        "ə" => "uh",
        "ᵊ" => "uh",
        "ɜ" => "ur",
        "e" => "e",
        "o" => "o",
        "ʃ" => "sh",
        "ʒ" => "zh",
        "θ" => "th",
        "ð" => "th",
        "ŋ" => "ng",
        "ʧ" => "ch",
        "ʤ" => "j",
        "j" => "y",
        "ɹ" => "r",
        "ɡ" => "g",
        _ => symbol,
    };

    private Dictionary<string, string> Load()
    {
        lock (_sync)
        {
            if (_entries is not null)
            {
                return _entries;
            }

            var entries = new Dictionary<string, string>(200_000, StringComparer.Ordinal);
            foreach (var line in File.ReadLines(_lexiconPath))
            {
                var space = line.IndexOf(' ');
                if (space <= 0 || space == line.Length - 1)
                {
                    continue;
                }

                entries.TryAdd(line[..space], line[(space + 1)..]);
            }

            _entries = entries;
            return entries;
        }
    }
}
