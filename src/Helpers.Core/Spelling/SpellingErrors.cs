namespace Helpers.Core.Spelling;

/// <summary>Pure helpers for keeping a list of spelling errors in step with the text and the caret.</summary>
public static class SpellingErrors
{
    /// <summary>
    /// Drops the error for the word the caret is in or has just finished, so a
    /// word is not marked wrong while it is still being typed. The caret sits
    /// between characters, so a caret at the word's end counts as inside it.
    /// </summary>
    public static IReadOnlyList<SpellingError> HideWordBeingTyped(IReadOnlyList<SpellingError> errors, int caretIndex)
    {
        if (errors.Count == 0)
        {
            return errors;
        }

        var kept = new List<SpellingError>(errors.Count);
        foreach (var error in errors)
        {
            if (caretIndex < error.Start || caretIndex > error.End)
            {
                kept.Add(error);
            }
        }

        return kept.Count == errors.Count ? errors : kept;
    }

    /// <summary>
    /// The word around a character index: letters, digits, apostrophes and
    /// hyphens, as the checker sees words. An index just after a word counts
    /// as on it. Null when the index is on a space or punctuation.
    /// </summary>
    public static SpellingError? WordAt(string text, int index)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var start = Math.Clamp(index, 0, text.Length);
        if (start == text.Length || !IsWordChar(text[start]))
        {
            if (start == 0 || !IsWordChar(text[start - 1]))
            {
                return null;
            }

            start--;
        }

        var end = start;
        while (start > 0 && IsWordChar(text[start - 1]))
        {
            start--;
        }

        while (end < text.Length && IsWordChar(text[end]))
        {
            end++;
        }

        return end > start ? new SpellingError(start, end - start) : null;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '\'' or '’' or '-';

    /// <summary>The error covering a character index, or the one that ends exactly there, or null.</summary>
    public static SpellingError? FindAt(IReadOnlyList<SpellingError> errors, int index)
    {
        foreach (var error in errors)
        {
            if (error.Contains(index) || error.End == index && error.Length > 0 && index > 0)
            {
                return error;
            }
        }

        return null;
    }

    /// <summary>
    /// Moves the errors from an old text onto a new one after one edit, so the
    /// underlines stay in place during the pause before the next check. Errors
    /// before the change keep their place; those after it shift by the change
    /// in length; any touching the change are dropped until the checker speaks.
    /// </summary>
    public static IReadOnlyList<SpellingError> Shift(IReadOnlyList<SpellingError> errors, string oldText, string newText)
    {
        if (errors.Count == 0 || oldText == newText)
        {
            return errors;
        }

        var shifted = new List<SpellingError>(errors.Count);
        foreach (var error in errors)
        {
            var (start, length) = ShiftRange(oldText, newText, error.Start, error.Length);
            if (start >= 0)
            {
                shifted.Add(new SpellingError(start, length));
            }
        }

        return shifted;
    }

    /// <summary>
    /// One range through one edit: unchanged before the edit, moved after it,
    /// and (-1, 0) when the edit touched it. Used for underlines and for the
    /// AI helper's notes alike.
    /// </summary>
    public static (int Start, int Length) ShiftRange(string oldText, string newText, int start, int length)
    {
        if (start < 0 || length <= 0)
        {
            return (-1, 0);
        }

        if (oldText == newText)
        {
            return (start, length);
        }

        var prefix = 0;
        var limit = Math.Min(oldText.Length, newText.Length);
        while (prefix < limit && oldText[prefix] == newText[prefix])
        {
            prefix++;
        }

        var suffix = 0;
        while (suffix < limit - prefix
               && oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
        {
            suffix++;
        }

        var oldChangeEnd = oldText.Length - suffix;
        var delta = newText.Length - oldText.Length;
        var end = start + length;

        if (end <= prefix)
        {
            return (start, length);
        }

        if (start >= oldChangeEnd)
        {
            return (start + delta, length);
        }

        return (-1, 0);
    }
}
