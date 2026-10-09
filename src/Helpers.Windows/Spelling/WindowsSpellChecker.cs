using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Helpers.Core.Spelling;

namespace Helpers.Windows.Spelling;

/// <summary>
/// The Windows spell-check engine, the same one Edge and Mail use. Free,
/// offline, and already on every PC that has the language installed.
/// Calls must come from the thread that created it; the UI thread is fine.
/// </summary>
public sealed class WindowsSpellChecker : ISpellChecker
{
    private const int MaxSuggestions = 8;

    private readonly ISpellCheckerCom? _checker;

    public WindowsSpellChecker(string languageTag = "en-GB")
    {
        LanguageTag = languageTag;
        try
        {
            var factory = (ISpellCheckerFactory)new SpellCheckerFactoryClass();
            if (factory.IsSupported(languageTag))
            {
                _checker = factory.CreateSpellChecker(languageTag);
            }
        }
        catch (COMException)
        {
            _checker = null;
        }
        catch (InvalidCastException)
        {
            _checker = null;
        }
    }

    public bool IsAvailable => _checker is not null;

    public string LanguageTag { get; }

    public IReadOnlyList<SpellingError> Check(string text)
    {
        if (_checker is null || string.IsNullOrEmpty(text))
        {
            return [];
        }

        var errors = new List<SpellingError>();
        var enumerator = _checker.Check(text);
        while (enumerator.Next(out var error) == 0 && error is not null)
        {
            try
            {
                var start = (int)error.GetStartIndex();
                var length = (int)error.GetLength();
                if (length > 0 && start >= 0 && start + length <= text.Length)
                {
                    errors.Add(new SpellingError(start, length));
                }
            }
            finally
            {
                Marshal.ReleaseComObject(error);
            }
        }

        Marshal.ReleaseComObject(enumerator);
        return errors;
    }

    public IReadOnlyList<string> Suggest(string word)
    {
        if (_checker is null || string.IsNullOrWhiteSpace(word))
        {
            return [];
        }

        var suggestions = new List<string>();
        var strings = _checker.Suggest(word);
        var buffer = new string[1];
        var fetched = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            while (strings.Next(1, buffer, fetched) == 0 && Marshal.ReadInt32(fetched) == 1)
            {
                if (!string.IsNullOrEmpty(buffer[0]))
                {
                    suggestions.Add(buffer[0]);
                }

                if (suggestions.Count >= MaxSuggestions)
                {
                    break;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(fetched);
            Marshal.ReleaseComObject(strings);
        }

        return suggestions;
    }

    public void Add(string word)
    {
        if (_checker is not null && !string.IsNullOrWhiteSpace(word))
        {
            _checker.Add(word.Trim());
        }
    }

    public void Ignore(string word)
    {
        if (_checker is not null && !string.IsNullOrWhiteSpace(word))
        {
            _checker.Ignore(word.Trim());
        }
    }
}
