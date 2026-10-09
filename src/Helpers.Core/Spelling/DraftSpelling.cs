namespace Helpers.Core.Spelling;

/// <summary>
/// Spelling for a draft being typed: the platform checker's errors, minus the
/// user's own words, minus the word under the caret. The UI calls
/// <see cref="Check"/> on a short pause after each keystroke.
/// </summary>
public sealed class DraftSpelling
{
    private readonly ISpellChecker _checker;
    private readonly UserDictionary _dictionary;

    public DraftSpelling(ISpellChecker checker, UserDictionary dictionary)
    {
        _checker = checker;
        _dictionary = dictionary;
        foreach (var word in dictionary.Words)
        {
            _checker.Ignore(word);
        }
    }

    /// <summary>False when the operating system has no checker for the language.</summary>
    public bool IsAvailable => _checker.IsAvailable;

    public string LanguageTag => _checker.LanguageTag;

    public IReadOnlyList<SpellingError> Check(string text, int caretIndex)
    {
        if (!_checker.IsAvailable || string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var errors = _checker.Check(text);
        if (_dictionary.Count > 0)
        {
            errors = errors.Where(e => !_dictionary.Contains(WordAt(text, e))).ToList();
        }

        return SpellingErrors.HideWordBeingTyped(errors, caretIndex);
    }

    public IReadOnlyList<string> Suggest(string word) => _checker.Suggest(word);

    /// <summary>Remembers the word for good, in the user's own file, and stops marking it now.</summary>
    public void AddToDictionary(string word)
    {
        if (_dictionary.Add(word))
        {
            _checker.Ignore(word);
        }
    }

    /// <summary>Stops marking the word until the app restarts.</summary>
    public void Ignore(string word) => _checker.Ignore(word);

    public static string WordAt(string text, SpellingError error) =>
        error.Start >= 0 && error.End <= text.Length ? text.Substring(error.Start, error.Length) : string.Empty;
}
