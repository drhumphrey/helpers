using System.Text.RegularExpressions;

namespace Helpers.Core.Text;

/// <summary>
/// Rewrites numbers the voice would otherwise read oddly. Years are the big
/// one: "1995" must be "nineteen ninety-five", not "one thousand nine hundred
/// and ninety-five".
/// </summary>
public static partial class NumberSpeech
{
    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
    ];

    private static readonly string[] Tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];

    /// <summary>Replaces four-digit years from 1500 to 2099 with their spoken form.</summary>
    public static string YearsAsSpeech(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return YearPattern().Replace(text, match =>
        {
            var year = int.Parse(match.Value);
            return year is >= 1500 and <= 2099 ? SayYear(year) : match.Value;
        });
    }

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

    // A four-digit number on its own: not part of a longer number, a decimal, a thousands group,
    // a price or a percentage. A full stop or comma after it, as in "in 1995.", is fine.
    [GeneratedRegex(@"(?<![\d£$€])(?<!\d[.,])\b(1[5-9]\d{2}|20\d{2})\b(?![\d%])(?![.,]\d)(?!\s*%)")]
    private static partial Regex YearPattern();
}
