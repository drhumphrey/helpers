namespace Helpers.Core.Ai;

public enum NoteKind
{
    /// <summary>The logic jumps; a step is missing.</summary>
    Logic,

    /// <summary>The reader needs context the writer left out.</summary>
    Missing,

    /// <summary>A word, name or reference is unclear.</summary>
    Unclear,

    /// <summary>A missing or wrong word.</summary>
    Wording,

    /// <summary>A spelling or grammar fix from Tidy.</summary>
    Grammar,

    Other,
}

/// <summary>
/// One note from the AI helper, pinned to a span of the user's text. A note
/// is a question, or a minimal fix, never a rewrite. Unanchored notes
/// (Start is -1) are shown without a highlight.
/// </summary>
public sealed record WritingNote(string Span, int Start, int Length, NoteKind Kind, string Note, string? Fix)
{
    public bool IsAnchored => Start >= 0 && Length > 0;

    public bool HasFix => !string.IsNullOrEmpty(Fix);

    public int End => Start + Length;

    /// <summary>For the note card: "Logic", "Missing", and so on.</summary>
    public string KindText => KindLabel(Kind);

    /// <summary>For the Apply button: the replacement words.</summary>
    public string FixText => Fix is null ? string.Empty : $"Apply: {Fix}";

    public static string KindLabel(NoteKind kind) => kind switch
    {
        NoteKind.Logic => "Logic",
        NoteKind.Missing => "Missing",
        NoteKind.Unclear => "Unclear",
        NoteKind.Wording => "Wording",
        NoteKind.Grammar => "Spelling or grammar",
        _ => "Note",
    };
}
