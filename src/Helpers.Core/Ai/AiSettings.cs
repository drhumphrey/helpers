namespace Helpers.Core.Ai;

public enum AiProvider
{
    /// <summary>The AI buttons are greyed out.</summary>
    None,

    /// <summary>A small model running on this PC. Nothing leaves the machine.</summary>
    Local,

    /// <summary>Claude, with the user's own API key. The text is sent to Anthropic.</summary>
    Cloud,
}

/// <summary>Everything about the AI helper that is safe to keep in the settings file. The API key is not.</summary>
public sealed class AiSettings
{
    public AiProvider Provider { get; set; } = AiProvider.None;

    /// <summary>The Claude model ID. Haiku is the cheapest and these are simple tasks.</summary>
    public string CloudModel { get; set; } = "claude-haiku-4-5";

    /// <summary>Drafts longer than this get a warning before they are sent.</summary>
    public int CloudInputLimit { get; set; } = 4000;

    /// <summary>Set once the user has seen what a cloud call sends and said yes.</summary>
    public bool CloudConfirmed { get; set; }

    /// <summary>Which local model file to use, by catalogue name.</summary>
    public string LocalModel { get; set; } = "Qwen3-4B-Q4_K_M";

    /// <summary>Folder for local model files. Null means the default under local app data.</summary>
    public string? LocalModelsFolder { get; set; }

    /// <summary>Minutes of silence before the local model is unloaded to free its memory. 0 keeps it loaded.</summary>
    public int UnloadLocalAfterMinutes { get; set; } = 10;

    /// <summary>System prompts the user has changed, by action name. Missing entries use the defaults.</summary>
    public Dictionary<string, string> Prompts { get; set; } = new(StringComparer.Ordinal);

    /// <summary>The month the spend counters belong to, as "2026-10". They reset when the month changes.</summary>
    public string? SpendMonth { get; set; }

    public long SpendInputTokens { get; set; }

    public long SpendOutputTokens { get; set; }

    /// <summary>The system prompt in force for an action: the user's version, or the default.</summary>
    public string PromptFor(AssistantAction action) =>
        Prompts.TryGetValue(action.ToString(), out var custom) && !string.IsNullOrWhiteSpace(custom)
            ? custom
            : PromptTemplates.Default(action);

    /// <summary>Adds a call's usage to this month's counters, starting fresh when the month has changed.</summary>
    public void RecordSpend(long inputTokens, long outputTokens, DateTime now)
    {
        var month = now.ToString("yyyy-MM");
        if (SpendMonth != month)
        {
            SpendMonth = month;
            SpendInputTokens = 0;
            SpendOutputTokens = 0;
        }

        SpendInputTokens += Math.Max(0, inputTokens);
        SpendOutputTokens += Math.Max(0, outputTokens);
    }
}
