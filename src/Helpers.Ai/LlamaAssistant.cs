using System.Runtime.Intrinsics.X86;
using Helpers.Core.Ai;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace Helpers.Ai;

/// <summary>
/// A small model running on this PC through LLamaSharp. Loads on first use,
/// which takes a few seconds, and unloads when told so its memory comes back.
/// Nothing leaves the machine.
/// </summary>
public sealed class LlamaAssistant : IAssistant
{
    private const int ContextSize = 4096;
    private readonly string _modelPath;
    private readonly string _modelName;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LLamaWeights? _weights;
    private ModelParams? _parameters;

    public LlamaAssistant(string modelPath, string modelName)
    {
        _modelPath = modelPath;
        _modelName = modelName;
    }

    public string Name => $"{_modelName} on this PC";

    public bool SendsTextOffMachine => false;

    public bool IsLoaded => _weights is not null;

    /// <summary>The CPU backend needs AVX2. Say so instead of crashing.</summary>
    public static bool CpuIsSupported => Avx2.IsSupported;

    public async Task<AssistantResult> RunAsync(string systemPrompt, string userMessage, IProgress<string>? partial, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_weights is null || _parameters is null)
            {
                _parameters = new ModelParams(_modelPath)
                {
                    ContextSize = ContextSize,
                    GpuLayerCount = 0,
                };
                _weights = await LLamaWeights.LoadFromFileAsync(_parameters, cancellationToken).ConfigureAwait(false);
            }

            // Qwen3 speaks ChatML. Thinking is switched off, or Tidy would take all day.
            var prompt =
                $"<|im_start|>system\n{systemPrompt}\n/no_think<|im_end|>\n" +
                $"<|im_start|>user\n{userMessage}<|im_end|>\n" +
                "<|im_start|>assistant\n<think>\n\n</think>\n\n";

            var executor = new StatelessExecutor(_weights, _parameters);
            var inference = new InferenceParams
            {
                MaxTokens = 1024,
                AntiPrompts = ["<|im_end|>", "<|im_start|>"],
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = 0.2f,
                    TopP = 0.9f,
                    RepeatPenalty = 1.05f,
                },
            };

            var builder = new System.Text.StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inference, cancellationToken).ConfigureAwait(false))
            {
                builder.Append(token);
                partial?.Report(builder.ToString());
            }

            var output = builder.ToString();
            foreach (var marker in inference.AntiPrompts)
            {
                var at = output.IndexOf(marker, StringComparison.Ordinal);
                if (at >= 0)
                {
                    output = output[..at];
                }
            }

            return new AssistantResult(output.Trim(), 0, 0);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Frees the model's memory. The next action loads it again.</summary>
    public void Unload()
    {
        _gate.Wait();
        try
        {
            _weights?.Dispose();
            _weights = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        Unload();
        _gate.Dispose();
    }
}
