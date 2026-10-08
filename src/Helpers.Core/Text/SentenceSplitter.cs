using System.Text;

namespace Helpers.Core.Text;

/// <summary>
/// Splits text into sentences the engine can take one at a time, without
/// breaking on "Dr.", "e.g.", "Fig. 2", decimals, version numbers or file names.
/// </summary>
public static class SentenceSplitter
{
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "dr", "mr", "mrs", "ms", "prof", "sr", "jr", "st", "vs", "etc", "e.g", "i.e", "al",
        "fig", "figs", "no", "nos", "approx", "dept", "inc", "ltd", "co", "cf", "eq", "ref",
        "sec", "vol", "p", "pp", "ch", "ed", "est", "min", "max", "ca", "viz", "mt", "rd", "ave",
        "jan", "feb", "mar", "apr", "jun", "jul", "aug", "sep", "sept", "oct", "nov", "dec",
    };

    private const string ClosersAfterPunctuation = "\"'’”)]";

    /// <summary>Splits the text into sentences, each no longer than <paramref name="maxLength"/>.</summary>
    public static IReadOnlyList<string> Split(string text, int maxLength = 400)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (maxLength < 20)
        {
            maxLength = 20;
        }

        var sentences = new List<string>();
        foreach (var line in text.Split('\n'))
        {
            foreach (var sentence in SplitLine(line))
            {
                sentences.AddRange(Chunk(sentence, maxLength));
            }
        }

        return sentences;
    }

    private static IEnumerable<string> SplitLine(string line)
    {
        var current = new StringBuilder();
        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            current.Append(c);

            if (c is '.' or '!' or '?')
            {
                // Swallow a run such as "?!" or "...".
                var j = i + 1;
                while (j < line.Length && line[j] is '.' or '!' or '?')
                {
                    current.Append(line[j]);
                    j++;
                }

                var singleFullStop = c == '.' && j - i == 1;

                // Closing quotes or brackets belong to this sentence.
                while (j < line.Length && ClosersAfterPunctuation.Contains(line[j]))
                {
                    current.Append(line[j]);
                    j++;
                }

                var atEnd = j >= line.Length;
                var followedBySpace = !atEnd && char.IsWhiteSpace(line[j]);
                if ((atEnd || followedBySpace) && !(singleFullStop && IsAbbreviationBefore(line, i)))
                {
                    var sentence = current.ToString().Trim();
                    if (sentence.Length > 0)
                    {
                        yield return sentence;
                    }

                    current.Clear();
                }

                i = j;
                continue;
            }

            i++;
        }

        var rest = current.ToString().Trim();
        if (rest.Length > 0)
        {
            yield return rest;
        }
    }

    /// <summary>True when the word just before the full stop at <paramref name="dotIndex"/> should not end a sentence.</summary>
    private static bool IsAbbreviationBefore(string line, int dotIndex)
    {
        var start = dotIndex;
        while (start > 0 && !char.IsWhiteSpace(line[start - 1]))
        {
            start--;
        }

        var token = line[start..dotIndex].TrimStart('(', '[', '"', '\'', '“', '‘');
        if (token.Length == 0)
        {
            return false;
        }

        if (Abbreviations.Contains(token))
        {
            return true;
        }

        // "1." at the start of a list item, or "No. 5": a short run of digits.
        if (token.Length <= 3 && token.All(char.IsDigit))
        {
            return true;
        }

        // An initial, as in "J. Smith".
        return token.Length == 1 && char.IsUpper(token[0]);
    }

    /// <summary>Breaks an over-long sentence at the most natural point before the limit.</summary>
    private static IEnumerable<string> Chunk(string sentence, int maxLength)
    {
        while (sentence.Length > maxLength)
        {
            var cut = FindCut(sentence, maxLength);
            yield return sentence[..cut].Trim();
            sentence = sentence[cut..].Trim();
        }

        if (sentence.Length > 0)
        {
            yield return sentence;
        }
    }

    private static int FindCut(string sentence, int maxLength)
    {
        foreach (var separator in new[] { "; ", ": ", ", ", " " })
        {
            var index = sentence.LastIndexOf(separator, maxLength, StringComparison.Ordinal);
            if (index > maxLength / 4)
            {
                return index + separator.Length;
            }
        }

        return maxLength;
    }
}
