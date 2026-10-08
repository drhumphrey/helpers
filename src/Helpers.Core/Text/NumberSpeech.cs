using System.Globalization;
using System.Text.RegularExpressions;

namespace Helpers.Core.Text;

/// <summary>
/// Rewrites numbers the voice would otherwise read left to right and get
/// wrong: money, percentages, clock times, ordinals and years. The engine
/// has no such layer of its own, so every case lives here, with a test.
/// </summary>
public static partial class NumberSpeech
{
    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
    ];

    private static readonly string[] Tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];

    private static readonly Dictionary<string, string> OrdinalOnes = new()
    {
        ["one"] = "first", ["two"] = "second", ["three"] = "third", ["four"] = "fourth", ["five"] = "fifth",
        ["six"] = "sixth", ["seven"] = "seventh", ["eight"] = "eighth", ["nine"] = "ninth", ["ten"] = "tenth",
        ["eleven"] = "eleventh", ["twelve"] = "twelfth", ["thirteen"] = "thirteenth", ["fourteen"] = "fourteenth",
        ["fifteen"] = "fifteenth", ["sixteen"] = "sixteenth", ["seventeen"] = "seventeenth", ["eighteen"] = "eighteenth",
        ["nineteen"] = "nineteenth", ["twenty"] = "twentieth", ["thirty"] = "thirtieth", ["forty"] = "fortieth",
        ["fifty"] = "fiftieth", ["sixty"] = "sixtieth", ["seventy"] = "seventieth", ["eighty"] = "eightieth",
        ["ninety"] = "ninetieth", ["hundred"] = "hundredth", ["thousand"] = "thousandth",
    };

    /// <summary>Applies every rule in the order that keeps them from tripping over each other.</summary>
    public static string Apply(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        text = MoneyAsSpeech(text);
        text = PercentAsSpeech(text);
        text = TimesAsSpeech(text);
        text = OrdinalsAsSpeech(text);
        text = YearsAsSpeech(text);
        return text;
    }

    /// <summary>"£895.00" becomes "eight hundred and ninety-five pounds"; "$0.99" becomes "ninety-nine cents".</summary>
    public static string MoneyAsSpeech(string text) =>
        MoneyPattern().Replace(text, match =>
        {
            var symbol = match.Groups["symbol"].Value;
            var whole = match.Groups["whole"].Value.Replace(",", string.Empty);
            var fraction = match.Groups["fraction"].Value;
            var scale = match.Groups["scale"].Value.ToLowerInvariant();

            var (major, majorOne, minor, minorOne) = symbol switch
            {
                "£" => ("pounds", "pound", "pence", "one penny"),
                "$" => ("dollars", "dollar", "cents", "one cent"),
                _ => ("euros", "euro", "cents", "one cent"),
            };

            if (!long.TryParse(whole, NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
            {
                return match.Value;
            }

            if (scale.Length > 0)
            {
                var number = fraction.Length > 0 ? $"{SayNumber(amount)} point {SayDigits(fraction)}" : SayNumber(amount);
                var word = scale switch
                {
                    "k" => "thousand",
                    "m" or "million" => "million",
                    _ => "billion",
                };
                return $"{number} {word} {major}";
            }

            var pence = fraction.Length == 0 ? 0 : int.Parse(fraction.PadRight(2, '0')[..2], CultureInfo.InvariantCulture);
            var majorText = amount == 1 ? $"one {majorOne}" : $"{SayNumber(amount)} {major}";
            if (pence == 0)
            {
                return majorText;
            }

            var minorText = pence == 1 ? minorOne : $"{SayNumber(pence)} {minor}";
            return amount == 0 ? minorText : $"{majorText} {SayNumber(pence)}";
        });

    /// <summary>"25%" becomes "twenty-five per cent"; "2.5%" becomes "two point five per cent".</summary>
    public static string PercentAsSpeech(string text) =>
        PercentPattern().Replace(text, match =>
        {
            var number = match.Groups["number"].Value;
            var dot = number.IndexOf('.');
            var spoken = dot < 0
                ? SayNumber(long.Parse(number.Replace(",", string.Empty), CultureInfo.InvariantCulture))
                : $"{SayNumber(long.Parse(number[..dot].Replace(",", string.Empty), CultureInfo.InvariantCulture))} point {SayDigits(number[(dot + 1)..])}";
            return spoken + " per cent";
        });

    /// <summary>"10:30" becomes "ten thirty", "10:05" "ten oh five", "9:00" "nine o'clock", "14:30 " "fourteen thirty".</summary>
    public static string TimesAsSpeech(string text) =>
        TimePattern().Replace(text, match =>
        {
            var hour = int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture);
            var minute = int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture);
            var suffix = match.Groups["suffix"].Value.ToLowerInvariant().Replace(".", string.Empty);
            var spokenSuffix = suffix switch
            {
                "am" => " a m",
                "pm" => " p m",
                _ => string.Empty,
            };

            var spoken = minute switch
            {
                0 => suffix.Length > 0 || hour > 12 ? SayNumber(hour) + " hundred" : SayNumber(hour) + " o'clock",
                < 10 => $"{SayNumber(hour)} oh {Ones[minute]}",
                _ => $"{SayNumber(hour)} {TwoDigits(minute)}",
            };

            if (minute == 0 && suffix.Length > 0)
            {
                spoken = SayNumber(hour);
            }

            return spoken + spokenSuffix;
        });

    /// <summary>"1st" becomes "first", "22nd" "twenty-second", "100th" "one hundredth".</summary>
    public static string OrdinalsAsSpeech(string text) =>
        OrdinalPattern().Replace(text, match =>
        {
            var number = long.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);
            var words = SayNumber(number);
            var lastSpace = words.LastIndexOf(' ');
            var lastHyphen = words.LastIndexOf('-');
            var cut = Math.Max(lastSpace, lastHyphen);
            var head = cut < 0 ? string.Empty : words[..(cut + 1)];
            var last = cut < 0 ? words : words[(cut + 1)..];
            return OrdinalOnes.TryGetValue(last, out var ordinal) ? head + ordinal : match.Value;
        });

    /// <summary>Replaces four-digit years from 1500 to 2099 with their spoken form.</summary>
    public static string YearsAsSpeech(string text) =>
        YearPattern().Replace(text, match =>
        {
            var year = int.Parse(match.Value, CultureInfo.InvariantCulture);
            return year is >= 1500 and <= 2099 ? SayYear(year) : match.Value;
        });

    /// <summary>How a year is said in British English.</summary>
    public static string SayYear(int year)
    {
        var century = year / 100;
        var rest = year % 100;

        if (century == 20)
        {
            return rest switch
            {
                0 => "two thousand",
                < 10 => "two thousand and " + Ones[rest],
                _ => "twenty " + TwoDigits(rest),
            };
        }

        return rest switch
        {
            0 => TwoDigits(century) + " hundred",
            < 10 => TwoDigits(century) + " oh " + Ones[rest],
            _ => TwoDigits(century) + " " + TwoDigits(rest),
        };
    }

    /// <summary>A whole number in British words: "one thousand two hundred and fifty".</summary>
    public static string SayNumber(long n)
    {
        if (n < 0)
        {
            return "minus " + SayNumber(-n);
        }

        if (n < 20)
        {
            return Ones[n];
        }

        if (n < 100)
        {
            return TwoDigits((int)n);
        }

        if (n < 1000)
        {
            var hundreds = Ones[n / 100] + " hundred";
            var rest = n % 100;
            return rest == 0 ? hundreds : hundreds + " and " + TwoDigits((int)rest);
        }

        foreach (var (value, name) in new[] { (1_000_000_000L, "billion"), (1_000_000L, "million"), (1_000L, "thousand") })
        {
            if (n >= value)
            {
                var head = SayNumber(n / value) + " " + name;
                var rest = n % value;
                if (rest == 0)
                {
                    return head;
                }

                return rest < 100 ? head + " and " + SayNumber(rest) : head + " " + SayNumber(rest);
            }
        }

        return n.ToString(CultureInfo.InvariantCulture);
    }

    private static string SayDigits(string digits) => string.Join(" ", digits.Select(d => char.IsDigit(d) ? Ones[d - '0'] : d.ToString()));

    private static string TwoDigits(int n)
    {
        if (n < 20)
        {
            return Ones[n];
        }

        var tens = Tens[n / 10];
        var ones = n % 10;
        return ones == 0 ? tens : $"{tens}-{Ones[ones]}";
    }

    [GeneratedRegex(@"(?<symbol>[£$€])\s?(?<whole>\d{1,3}(?:,\d{3})+|\d+)(?:\.(?<fraction>\d{1,2}))?\s?(?<scale>k|m|bn|million|billion)?(?!\w)(?!\.\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MoneyPattern();

    [GeneratedRegex(@"(?<![\w.])(?<number>\d{1,3}(?:,\d{3})+|\d+(?:\.\d+)?)\s?%", RegexOptions.CultureInvariant)]
    private static partial Regex PercentPattern();

    [GeneratedRegex(@"\b(?<hour>[01]?\d|2[0-3]):(?<minute>[0-5]\d)(?!:|\d)(?:\s?(?<suffix>[ap]\.?m)(?!\w))?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TimePattern();

    [GeneratedRegex(@"\b(?<number>\d{1,4})(?:st|nd|rd|th)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrdinalPattern();

    // A four-digit number on its own: not part of a longer number, a decimal, a thousands group,
    // a price or a percentage. A full stop or comma after it, as in "in 1995.", is fine.
    [GeneratedRegex(@"(?<![\d£$€])(?<!\d[.,])\b(1[5-9]\d{2}|20\d{2})\b(?![\d%])(?![.,]\d)(?!\s*%)")]
    private static partial Regex YearPattern();
}
