namespace Helpers.Core.Ai;

/// <summary>
/// Turns token counts into money, so the Settings page can say what the
/// cloud helper has cost this month. Prices are per million tokens in US
/// dollars, as Anthropic publishes them; they are an estimate, not a bill.
/// </summary>
public static class CloudCost
{
    private sealed record Price(decimal InputPerMillion, decimal OutputPerMillion);

    private static readonly Dictionary<string, Price> Prices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-haiku-4-5"] = new(1.00m, 5.00m),
        ["claude-sonnet-4-5"] = new(3.00m, 15.00m),
        ["claude-sonnet-5-5"] = new(3.00m, 15.00m),
        ["claude-opus-5-5"] = new(15.00m, 75.00m),
    };

    private static readonly Price Unknown = new(3.00m, 15.00m);

    /// <summary>True when the price of the model is known rather than guessed.</summary>
    public static bool IsKnown(string model) => Match(model) is not null;

    public static decimal EstimateUsd(string model, long inputTokens, long outputTokens)
    {
        var price = Match(model) ?? Unknown;
        return (inputTokens * price.InputPerMillion + outputTokens * price.OutputPerMillion) / 1_000_000m;
    }

    /// <summary>"$0.03" or "less than a cent".</summary>
    public static string Describe(decimal usd)
    {
        if (usd <= 0)
        {
            return "nothing";
        }

        return usd < 0.01m ? "less than a cent" : $"${usd:0.00}";
    }

    /// <summary>Matches "claude-haiku-4-5-20251001" to "claude-haiku-4-5".</summary>
    private static Price? Match(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        foreach (var (name, price) in Prices)
        {
            if (model.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                return price;
            }
        }

        return null;
    }
}
