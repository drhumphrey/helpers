namespace Helpers.Core.Spelling;

/// <summary>One misspelt word: where it is in the text that was checked.</summary>
public sealed record SpellingError(int Start, int Length)
{
    public int End => Start + Length;

    public bool Contains(int index) => index >= Start && index < End;
}

/// <summary>
/// A spelling engine. On Windows this wraps the system checker; on Mac it
/// will wrap the Mac's. Core and the UI only talk to this.
/// </summary>
public interface ISpellChecker
{
    /// <summary>False when the OS has no checker for the language, in which case every call returns nothing.</summary>
    bool IsAvailable { get; }

    string LanguageTag { get; }

    IReadOnlyList<SpellingError> Check(string text);

    IReadOnlyList<string> Suggest(string word);

    /// <summary>Adds a word to the user's dictionary for good.</summary>
    void Add(string word);

    /// <summary>Ignores a word for this session.</summary>
    void Ignore(string word);
}
