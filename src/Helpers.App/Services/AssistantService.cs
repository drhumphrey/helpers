using System.Security.Cryptography;
using System.Text;
using Avalonia.Threading;
using Helpers.Ai;
using Helpers.Core.Ai;
using Helpers.Core.Settings;

namespace Helpers.App.Services;

/// <summary>
/// The one door to the AI helper for the rest of the app. Builds the right
/// provider from settings, caches answers for the session, records cloud
/// spend, and unloads the local model when it has been idle for a while.
/// </summary>
public sealed class AssistantService : IDisposable
{
    public const string KeyName = "anthropic-api-key";

    private readonly SettingsStore _settings;
    private readonly ISecretStore _secrets;
    private readonly Dictionary<string, AssistantResult> _cache = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _unloadTimer;
    private IAssistant? _assistant;
    private string _builtFor = string.Empty;
    private CancellationTokenSource? _download;

    public AssistantService(SettingsStore settings, ISecretStore secrets)
    {
        _settings = settings;
        _secrets = secrets;
        _unloadTimer = new DispatcherTimer(TimeSpan.FromMinutes(10), DispatcherPriority.Background, (_, _) => UnloadLocal());
        _unloadTimer.Stop();
    }

    /// <summary>Raised when the provider changes or the local model finishes downloading, so buttons can update.</summary>
    public event Action? Changed;

    public AiProvider Provider => _settings.Current.Ai.Provider;

    public bool SendsTextOffMachine => Provider == AiProvider.Cloud;

    /// <summary>True when an action can run right now.</summary>
    public bool IsReady => WhyNotReady() is null;

    /// <summary>A short name for the status line, such as "Claude" or "Qwen3 on this PC".</summary>
    public string Name => Provider switch
    {
        AiProvider.Cloud => "Claude",
        AiProvider.Local => $"{LocalModels.ByName(_settings.Current.Ai.LocalModel).Name} on this PC",
        _ => "no AI helper",
    };

    public string LocalModelsFolder => _settings.Current.Ai.LocalModelsFolder ?? Path.Combine(SettingsStore.DefaultModelsFolder(), "llm");

    public bool LocalModelDownloaded => LocalModels.IsDownloaded(LocalModels.ByName(_settings.Current.Ai.LocalModel), LocalModelsFolder);

    public bool IsDownloading => _download is not null;

    public bool HasCloudKey => !string.IsNullOrWhiteSpace(_secrets.Get(KeyName));

    /// <summary>Null when ready; otherwise one line saying what is missing, for a hint under the greyed buttons.</summary>
    public string? WhyNotReady()
    {
        var ai = _settings.Current.Ai;
        switch (ai.Provider)
        {
            case AiProvider.None:
                return "Choose an AI helper in Settings to use this.";
            case AiProvider.Cloud:
                return HasCloudKey ? null : "Add your Claude API key in Settings to use this.";
            case AiProvider.Local:
                if (!LlamaAssistant.CpuIsSupported)
                {
                    return "This PC's processor can't run the local model. Claude in the cloud still works.";
                }

                return LocalModelDownloaded ? null : "Download the local model in Settings to use this.";
            default:
                return "Choose an AI helper in Settings to use this.";
        }
    }

    /// <summary>Runs an action on the text. Throws on provider errors; callers turn those into toasts.</summary>
    public async Task<AssistantResult> RunAsync(AssistantAction action, string text, IProgress<string>? partial, CancellationToken cancellationToken)
    {
        var why = WhyNotReady();
        if (why is not null)
        {
            throw new InvalidOperationException(why);
        }

        var ai = _settings.Current.Ai;
        var systemPrompt = ai.PromptFor(action);
        var userMessage = PromptTemplates.UserMessage(action, text);
        var assistant = Build();
        var key = CacheKey(action, text, systemPrompt);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        _unloadTimer.Stop();
        var result = await assistant.RunAsync(systemPrompt, userMessage, partial, cancellationToken);
        _cache[key] = result;

        if (assistant.SendsTextOffMachine && (result.InputTokens > 0 || result.OutputTokens > 0))
        {
            _settings.Update(s => s.Ai.RecordSpend(result.InputTokens, result.OutputTokens, DateTime.Now));
        }

        if (assistant is LlamaAssistant && ai.UnloadLocalAfterMinutes > 0)
        {
            _unloadTimer.Interval = TimeSpan.FromMinutes(ai.UnloadLocalAfterMinutes);
            _unloadTimer.Start();
        }

        return result;
    }

    /// <summary>Forgets the built provider and the cache, after a settings change.</summary>
    public void Rebuild()
    {
        _assistant?.Dispose();
        _assistant = null;
        _builtFor = string.Empty;
        _cache.Clear();
        Changed?.Invoke();
    }

    public void SetCloudKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _secrets.Remove(KeyName);
        }
        else
        {
            _secrets.Set(KeyName, key.Trim());
        }

        Rebuild();
    }

    /// <summary>Downloads the chosen local model with progress 0 to 1. Only one download at a time.</summary>
    public async Task DownloadLocalModelAsync(IProgress<double> progress)
    {
        if (_download is not null)
        {
            return;
        }

        _download = new CancellationTokenSource();
        try
        {
            var model = LocalModels.ByName(_settings.Current.Ai.LocalModel);
            await LocalModels.DownloadAsync(model, LocalModelsFolder, progress, _download.Token);
            Changed?.Invoke();
        }
        finally
        {
            _download.Dispose();
            _download = null;
        }
    }

    public void CancelDownload() => _download?.Cancel();

    public void UnloadLocal()
    {
        _unloadTimer.Stop();
        (_assistant as LlamaAssistant)?.Unload();
    }

    public void Dispose()
    {
        _download?.Cancel();
        _unloadTimer.Stop();
        _assistant?.Dispose();
        _assistant = null;
    }

    private IAssistant Build()
    {
        var ai = _settings.Current.Ai;
        var signature = ai.Provider switch
        {
            AiProvider.Cloud => $"cloud|{ai.CloudModel}",
            AiProvider.Local => $"local|{ai.LocalModel}|{LocalModelsFolder}",
            _ => "none",
        };

        if (_assistant is not null && _builtFor == signature)
        {
            return _assistant;
        }

        _assistant?.Dispose();
        _assistant = ai.Provider switch
        {
            AiProvider.Cloud => new AnthropicAssistant(_secrets.Get(KeyName) ?? string.Empty, ai.CloudModel),
            AiProvider.Local => new LlamaAssistant(LocalModels.PathFor(LocalModels.ByName(ai.LocalModel), LocalModelsFolder), LocalModels.ByName(ai.LocalModel).Name),
            _ => throw new InvalidOperationException("Choose an AI helper in Settings to use this."),
        };
        _builtFor = signature;
        return _assistant;
    }

    private string CacheKey(AssistantAction action, string text, string systemPrompt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{_builtFor}\n{action}\n{systemPrompt}\n{text}"));
        return Convert.ToHexString(bytes);
    }
}
