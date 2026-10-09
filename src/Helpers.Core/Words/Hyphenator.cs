using System.Reflection;
using System.Text;

namespace Helpers.Core.Words;

/// <summary>
/// Splits an English word into its hyphenation chunks, which is near enough
/// to syllables for seeing a long word in pieces: re·gis·tra·tion. Liang's
/// algorithm over the TeX British English patterns bundled with the app.
/// </summary>
public sealed class Hyphenator
{
    private const int LeftMin = 2;
    private const int RightMin = 3;

    private readonly Dictionary<string, int[]> _patterns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _exceptions = new(StringComparer.Ordinal);
    private readonly int _longestPattern;

    private static readonly Lazy<Hyphenator> BritishLazy = new(() => FromResource("Helpers.Core.Resources.hyph-en-gb.tex"));

    /// <summary>The British English hyphenator, loaded once.</summary>
    public static Hyphenator British => BritishLazy.Value;

    public Hyphenator(string texPatterns)
    {
        var (patterns, exceptions) = Parse(texPatterns);
        foreach (var pattern in patterns)
        {
            var (letters, points) = Decode(pattern);
            if (letters.Length > 0)
            {
                _patterns[letters] = points;
                _longestPattern = Math.Max(_longestPattern, letters.Length);
            }
        }

        foreach (var exception in exceptions)
        {
            var parts = exception.Split('-', StringSplitOptions.RemoveEmptyEntries);
            _exceptions[string.Concat(parts)] = parts;
        }
    }

    public static Hyphenator FromResource(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing resource {resourceName}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return new Hyphenator(reader.ReadToEnd());
    }

    public int PatternCount => _patterns.Count;

    /// <summary>The word in pieces. A word the patterns can't split, or one with odd characters, comes back whole.</summary>
    public IReadOnlyList<string> Syllables(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return [];
        }

        var trimmed = word.Trim();
        var lower = trimmed.ToLowerInvariant();
        if (lower.Any(c => c is < 'a' or > 'z'))
        {
            return [trimmed];
        }

        if (_exceptions.TryGetValue(lower, out var known))
        {
            return Recase(trimmed, known);
        }

        if (lower.Length < LeftMin + RightMin)
        {
            return [trimmed];
        }

        var padded = "." + lower + ".";
        var points = new int[padded.Length + 1];
        for (var start = 0; start < padded.Length; start++)
        {
            var maxLength = Math.Min(_longestPattern, padded.Length - start);
            for (var length = 1; length <= maxLength; length++)
            {
                if (!_patterns.TryGetValue(padded.Substring(start, length), out var values))
                {
                    continue;
                }

                for (var i = 0; i < values.Length; i++)
                {
                    points[start + i] = Math.Max(points[start + i], values[i]);
                }
            }
        }

        // points[k] sits before padded[k]; padded has a leading dot, so letter j of the word is at padded index j + 1.
        var pieces = new List<string>();
        var pieceStart = 0;
        for (var j = 1; j < lower.Length; j++)
        {
            var allowed = points[j + 1] % 2 == 1 && j >= LeftMin && lower.Length - j >= RightMin;
            if (allowed)
            {
                pieces.Add(trimmed[pieceStart..j]);
                pieceStart = j;
            }
        }

        pieces.Add(trimmed[pieceStart..]);
        return pieces;
    }

    /// <summary>"re·gis·tra·tion", with the separator of your choice.</summary>
    public string Hyphenate(string word, string separator = "·") => string.Join(separator, Syllables(word));

    private static IReadOnlyList<string> Recase(string original, string[] lowerPieces)
    {
        var result = new List<string>(lowerPieces.Length);
        var at = 0;
        foreach (var piece in lowerPieces)
        {
            result.Add(original.Substring(at, Math.Min(piece.Length, original.Length - at)));
            at += piece.Length;
        }

        return result;
    }

    private static (string Letters, int[] Points) Decode(string pattern)
    {
        var letters = new StringBuilder();
        var points = new List<int> { 0 };
        foreach (var c in pattern)
        {
            if (char.IsDigit(c))
            {
                points[^1] = c - '0';
            }
            else
            {
                letters.Append(c);
                points.Add(0);
            }
        }

        return (letters.ToString(), points.ToArray());
    }

    private static (List<string> Patterns, List<string> Exceptions) Parse(string tex)
    {
        var patterns = new List<string>();
        var exceptions = new List<string>();
        List<string>? current = null;

        foreach (var rawLine in tex.Split('\n'))
        {
            var line = rawLine;
            var comment = line.IndexOf('%');
            if (comment >= 0)
            {
                line = line[..comment];
            }

            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("\\patterns", StringComparison.Ordinal))
            {
                current = patterns;
                line = line[(line.IndexOf('{') + 1)..];
            }
            else if (line.StartsWith("\\hyphenation", StringComparison.Ordinal))
            {
                current = exceptions;
                line = line[(line.IndexOf('{') + 1)..];
            }

            if (current is null)
            {
                continue;
            }

            var closed = line.IndexOf('}');
            if (closed >= 0)
            {
                line = line[..closed];
            }

            foreach (var token in line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                current.Add(token);
            }

            if (closed >= 0)
            {
                current = null;
            }
        }

        return (patterns, exceptions);
    }
}
