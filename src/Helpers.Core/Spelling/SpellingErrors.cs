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

        var shifted = new List<SpellingError>(errors.Count);
        foreach (var error in errors)
        {
            if (error.End <= prefix)
            {
                shifted.Add(error);
            }
            else if (error.Start >= oldChangeEnd)
            {
                shifted.Add(error with { Start = error.Start + delta });
            }
        }

        return shifted;
    }
}
