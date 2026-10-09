namespace Helpers.Core.Words;

/// <summary>Everything the word tools show for one word.</summary>
public sealed record WordInfo(
    string Word,
    IReadOnlyList<string> Syllables,
    string? Ipa,
    string? Respelling,
    IReadOnlyList<WordMeaning> Meanings)
{
    public string SyllableText => string.Join(" · ", Syllables);

    public bool HasPronunciation => !string.IsNullOrEmpty(Ipa);

    public bool HasMeanings => Meanings.Count > 0;

    /// <summary>All the synonyms across the senses, without repeats, for the chips.</summary>
    public IReadOnlyList<string> AllSynonyms => Meanings
        .SelectMany(m => m.Synonyms)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(16)
        .ToList();
}

/// <summary>Syllables from the bundled patterns, sounds from the voice's list, meanings from WordNet.</summary>
public sealed class WordLookup
{
    private readonly Hyphenator _hyphenator;
    private readonly Pronunciation _pronunciation;
    private readonly WordNet _wordNet;

    public WordLookup(Hyphenator hyphenator, Pronunciation pronunciation, WordNet wordNet)
    {
        _hyphenator = hyphenator;
        _pronunciation = pronunciation;
        _wordNet = wordNet;
    }

    public bool DictionaryAvailable => _wordNet.IsAvailable;

    public bool PronunciationAvailable => _pronunciation.IsAvailable;

    public WordInfo Lookup(string word)
    {
        var trimmed = word.Trim().Trim('.', ',', ';', ':', '!', '?', '"', '“', '”', '(', ')', '[', ']');
        return new WordInfo(
            trimmed,
            _hyphenator.Syllables(trimmed),
            _pronunciation.Ipa(trimmed),
            _pronunciation.Respelling(trimmed),
            _wordNet.Lookup(trimmed));
    }
}
