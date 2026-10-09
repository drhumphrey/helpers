namespace Helpers.Core.Ai;

/// <summary>The five things the AI helper can do. Each is a prompt template behind the same plumbing.</summary>
public enum AssistantAction
{
    /// <summary>A reader's notes: where the logic jumps, what is missing, what is unclear. Never a rewrite.</summary>
    CheckMyThinking,

    /// <summary>Spelling and grammar only, each change marked.</summary>
    Tidy,

    /// <summary>Rewrite notes as a clear request to a coding assistant.</summary>
    MakeARequest,

    /// <summary>The main points in a few bullets.</summary>
    Summarise,

    /// <summary>The same text for a non-technical reader.</summary>
    ExplainSimply,
}

public static class AssistantActions
{
    public static readonly AssistantAction[] All =
    [
        AssistantAction.CheckMyThinking,
        AssistantAction.Tidy,
        AssistantAction.MakeARequest,
        AssistantAction.Summarise,
        AssistantAction.ExplainSimply,
    ];

    public static string Title(AssistantAction action) => action switch
    {
        AssistantAction.CheckMyThinking => "Check my thinking",
        AssistantAction.Tidy => "Tidy",
        AssistantAction.MakeARequest => "Make a request",
        AssistantAction.Summarise => "Summarise",
        AssistantAction.ExplainSimply => "Explain simply",
        _ => action.ToString(),
    };

    /// <summary>True for the actions whose answer is a list of notes pinned to the text rather than new text.</summary>
    public static bool ReturnsNotes(AssistantAction action) => action is AssistantAction.CheckMyThinking or AssistantAction.Tidy;
}
