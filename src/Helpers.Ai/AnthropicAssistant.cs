using Anthropic;
using Anthropic.Models.Messages;
using Helpers.Core.Ai;

namespace Helpers.Ai;

/// <summary>
/// Claude through the official SDK, with the user's own key. One request
/// per action: the system prompt and the text, no history, nothing else.
/// </summary>
public sealed class AnthropicAssistant : IAssistant
{
    private readonly AnthropicClient _client;
    private readonly string _model;

    public AnthropicAssistant(string apiKey, string model)
    {
        // The address is pinned. The SDK would otherwise honour an ANTHROPIC_BASE_URL environment
        // variable, which could quietly send the user's text and key somewhere else.
        _client = new AnthropicClient
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.anthropic.com",
            MaxRetries = 2,
            Timeout = TimeSpan.FromSeconds(90),
        };
        _model = model;
    }

    public string Name => "Claude";

    public bool SendsTextOffMachine => true;

    public async Task<AssistantResult> RunAsync(string systemPrompt, string userMessage, IProgress<string>? partial, CancellationToken cancellationToken)
    {
        var parameters = new MessageCreateParams
        {
            Model = _model,
            MaxTokens = Math.Max(256, Math.Min(4096, userMessage.Length / 2)),
            System = systemPrompt,
            Messages =
            [
                new() { Role = Role.User, Content = userMessage },
            ],
        };

        var builder = new System.Text.StringBuilder();
        long inputTokens = 0;
        long outputTokens = 0;

        await foreach (var chunk in _client.Messages.CreateStreaming(parameters, cancellationToken))
        {
            if (chunk.TryPickStart(out var start))
            {
                inputTokens = start.Message.Usage.InputTokens;
            }
            else if (chunk.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
            {
                builder.Append(text.Text);
                partial?.Report(builder.ToString());
            }
            else if (chunk.TryPickDelta(out var messageDelta))
            {
                outputTokens = messageDelta.Usage.OutputTokens;
            }
        }

        return new AssistantResult(builder.ToString(), inputTokens, outputTokens);
    }

    public void Dispose()
    {
    }
}
