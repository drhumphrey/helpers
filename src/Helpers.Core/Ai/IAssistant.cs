namespace Helpers.Core.Ai;

/// <param name="Output">The model's whole answer.</param>
/// <param name="InputTokens">Tokens sent, as the provider counts them. Zero when unknown.</param>
/// <param name="OutputTokens">Tokens received. Zero when unknown.</param>
public sealed record AssistantResult(string Output, long InputTokens, long OutputTokens);

/// <summary>
/// A language model behind one door. The local provider runs on this PC;
/// the cloud provider sends the text away, and says so.
/// </summary>
public interface IAssistant : IDisposable
{
    /// <summary>A short name for the UI, such as "Claude" or "Qwen on this PC".</summary>
    string Name { get; }

    /// <summary>True when running an action sends the text off the machine.</summary>
    bool SendsTextOffMachine { get; }

    /// <summary>
    /// Runs one action. The system prompt and the user message are built by
    /// the caller from the templates, so the user's edits to them apply.
    /// Partial text arrives through <paramref name="partial"/> as it streams.
    /// </summary>
    Task<AssistantResult> RunAsync(string systemPrompt, string userMessage, IProgress<string>? partial, CancellationToken cancellationToken);
}

/// <summary>Where secrets such as the cloud API key live. Never the settings file.</summary>
public interface ISecretStore
{
    string? Get(string name);

    void Set(string name, string value);

    void Remove(string name);
}
