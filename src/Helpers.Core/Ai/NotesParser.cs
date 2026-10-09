using System.Text.Json;

namespace Helpers.Core.Ai;

/// <summary>
/// Turns the model's answer into notes pinned to the draft. Forgiving about
/// the wrapping (code fences, chatter before or after the JSON) and about
/// spans that don't match exactly (case, spacing, quotes). A note whose span
/// can't be found is kept, unanchored, rather than thrown away.
/// </summary>
public static class NotesParser
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public static IReadOnlyList<WritingNote> Parse(string modelOutput, string draft, AssistantAction action)
    {
        var json = ExtractArray(modelOutput);
        if (json is null)
        {
            return [];
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, DocumentOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var notes = new List<WritingNote>();
            var searchFrom = 0;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var span = GetString(element, "span") ?? string.Empty;
                var fix = GetString(element, "fix");
                var note = GetString(element, "note") ?? string.Empty;
                var kind = ParseKind(GetString(element, "kind"), action);

                if (action == AssistantAction.Tidy && note.Length == 0 && fix is not null)
                {
                    note = $"Change to “{fix}”";
                }

                if (span.Length == 0 && note.Length == 0)
                {
                    continue;
                }

                var (start, length) = SpanAnchor.Find(draft, span, searchFrom);
                if (start >= 0)
                {
                    searchFrom = start + length;
                }

                notes.Add(new WritingNote(span, start, length, kind, note, string.IsNullOrWhiteSpace(fix) ? null : fix));
            }

            return notes;
        }
    }

    /// <summary>The JSON array inside the answer: from the first '[' to the last ']', with code fences ignored.</summary>
    public static string? ExtractArray(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var start = output.IndexOf('[');
        var end = output.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            return output.Trim() == "[]" ? "[]" : null;
        }

        return output.Substring(start, end - start + 1);
    }

    private static string? GetString(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Null => null,
                    _ => property.Value.ToString(),
                };
            }
        }

        return null;
    }

    private static NoteKind ParseKind(string? kind, AssistantAction action)
    {
        if (action == AssistantAction.Tidy)
        {
            return NoteKind.Grammar;
        }

        return kind?.Trim().ToLowerInvariant() switch
        {
            "logic" => NoteKind.Logic,
            "missing" or "context" => NoteKind.Missing,
            "unclear" or "reference" => NoteKind.Unclear,
            "wording" or "word" or "spelling" or "grammar" => NoteKind.Wording,
            _ => NoteKind.Other,
        };
    }
}

/// <summary>Finds a quoted span in the draft, exactly first, then more loosely.</summary>
public static class SpanAnchor
{
    public static (int Start, int Length) Find(string text, string span, int from)
    {
        if (string.IsNullOrWhiteSpace(span) || string.IsNullOrEmpty(text))
        {
            return (-1, 0);
        }

        from = Math.Clamp(from, 0, text.Length);
        var trimmed = span.Trim();

        var index = text.IndexOf(trimmed, from, StringComparison.Ordinal);
        if (index < 0)
        {
            index = text.IndexOf(trimmed, StringComparison.Ordinal);
        }

        if (index >= 0)
        {
            return (index, trimmed.Length);
        }

        index = text.IndexOf(trimmed, from, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            index = text.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase);
        }

        if (index >= 0)
        {
            return (index, trimmed.Length);
        }

        var loose = FindLoosely(text, trimmed);
        return loose.Start >= 0 ? loose : FindFuzzy(text, trimmed);
    }

    /// <summary>
    /// Models sometimes mis-copy a word by a letter or two ("ofern" for
    /// "oftern"). Looks for the window of the text closest to the span, and
    /// takes it when the difference is small compared with the span's length.
    /// </summary>
    private static (int Start, int Length) FindFuzzy(string text, string span)
    {
        if (span.Length < 4 || span.Length > 160 || text.Length < span.Length)
        {
            return (-1, 0);
        }

        var allowed = Math.Max(1, span.Length / 8);
        var bestDistance = int.MaxValue;
        var best = (Start: -1, Length: 0);

        for (var start = 0; start + span.Length - allowed <= text.Length; start++)
        {
            // Cheap filter: the first letters should agree before paying for a full comparison.
            if (char.ToLowerInvariant(text[start]) != char.ToLowerInvariant(span[0]))
            {
                continue;
            }

            for (var length = span.Length - allowed; length <= span.Length + allowed; length++)
            {
                if (length <= 0 || start + length > text.Length)
                {
                    continue;
                }

                var distance = Levenshtein(span, text.AsSpan(start, length), bestDistance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (start, length);
                }
            }
        }

        return bestDistance <= allowed ? best : (-1, 0);
    }

    private static int Levenshtein(string a, ReadOnlySpan<char> b, int giveUpAt)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            var rowMin = current[0];
            var ca = char.ToLowerInvariant(a[i - 1]);
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = ca == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                rowMin = Math.Min(rowMin, current[j]);
            }

            if (rowMin >= giveUpAt)
            {
                return giveUpAt;
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    /// <summary>Matches ignoring runs of whitespace and the difference between straight and curly quotes.</summary>
    private static (int Start, int Length) FindLoosely(string text, string span)
    {
        var (normalText, map) = Normalise(text);
        var (normalSpan, _) = Normalise(span);
        if (normalSpan.Length == 0)
        {
            return (-1, 0);
        }

        var index = normalText.IndexOf(normalSpan, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return (-1, 0);
        }

        var start = map[index];
        var end = map[index + normalSpan.Length - 1] + 1;
        return (start, end - start);
    }

    private static (string Text, int[] Map) Normalise(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        var map = new List<int>(text.Length);
        var lastWasSpace = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                if (lastWasSpace)
                {
                    continue;
                }

                lastWasSpace = true;
                builder.Append(' ');
                map.Add(i);
                continue;
            }

            lastWasSpace = false;
            builder.Append(c switch
            {
                '‘' or '’' => '\'',
                '“' or '”' => '"',
                _ => c,
            });
            map.Add(i);
        }

        return (builder.ToString().Trim(), map.ToArray());
    }
}
