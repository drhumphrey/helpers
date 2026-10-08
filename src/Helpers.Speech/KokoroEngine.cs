using Helpers.Core.Speech;
using SherpaOnnx;

namespace Helpers.Speech;

/// <summary>
/// Kokoro running in sherpa-onnx, fully offline. One model instance, used
/// from one call at a time; the gate serialises load, synthesis and unload.
/// </summary>
public sealed class KokoroEngine : ISpeechEngine
{
    /// <summary>The only sherpa-onnx Kokoro package with the British voices.</summary>
    public const string PackageName = "kokoro-multi-lang-v1_0";

    public const string DownloadUrl =
        "https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/" + PackageName + ".tar.bz2";

    private const int DefaultSampleRate = 24000;

    private static readonly SpeechVoice[] BritishVoices =
    [
        new("bf_alice", "Alice", 20),
        new("bf_emma", "Emma", 21),
        new("bf_isabella", "Isabella", 22),
        new("bf_lily", "Lily", 23),
        new("bm_daniel", "Daniel", 24),
        new("bm_fable", "Fable", 25),
        new("bm_george", "George", 26),
        new("bm_lewis", "Lewis", 27),
    ];

    private readonly string _modelsRoot;
    private readonly int _threads;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private OfflineTts? _tts;

    public KokoroEngine(string modelsRoot, int threads = 4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelsRoot);
        _modelsRoot = modelsRoot;
        _threads = Math.Clamp(threads, 1, 16);
    }

    public bool IsLoaded => _tts is not null;

    public int SampleRate => _tts?.SampleRate ?? DefaultSampleRate;

    public IReadOnlyList<SpeechVoice> Voices => BritishVoices;

    public static SpeechVoice DefaultVoice => BritishVoices[6];

    public static SpeechVoice? FindVoice(string id) =>
        BritishVoices.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase));

    public async Task LoadAsync(IProgress<EngineProgress>? progress, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_tts is not null)
            {
                return;
            }

            await ModelDownloader.EnsureAsync(_modelsRoot, PackageName, DownloadUrl, progress, cancellationToken).ConfigureAwait(false);
            progress?.Report(new EngineProgress("Loading the voice", null));
            var modelDir = Path.Combine(_modelsRoot, PackageName);
            _tts = await Task.Run(() => Create(modelDir, _threads), cancellationToken).ConfigureAwait(false);
            progress?.Report(new EngineProgress("Voice ready", 1));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AudioClip> SynthesiseAsync(SynthesisRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return new AudioClip([], SampleRate);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tts = _tts ?? throw new InvalidOperationException("The voice has not been loaded.");
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var config = new OfflineTtsGenerationConfig
                {
                    Sid = request.Voice.SpeakerId,
                    Speed = Math.Clamp(request.Speed, 0.5f, 2.0f),
                };

                var audio = tts.GenerateWithConfig(request.Text, config, KeepGoing);
                return new AudioClip(audio.Samples, audio.SampleRate);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Unload()
    {
        _gate.Wait();
        try
        {
            _tts?.Dispose();
            _tts = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static readonly OfflineTtsCallbackProgressWithArg KeepGoing = (_, _, _, _) => 1;

    private static OfflineTts Create(string modelDir, int threads)
    {
        var config = new OfflineTtsConfig();
        config.Model.Kokoro.Model = Path.Combine(modelDir, "model.onnx");
        config.Model.Kokoro.Voices = Path.Combine(modelDir, "voices.bin");
        config.Model.Kokoro.Tokens = Path.Combine(modelDir, "tokens.txt");
        config.Model.Kokoro.DataDir = Path.Combine(modelDir, "espeak-ng-data");
        config.Model.Kokoro.Lexicon = Path.Combine(modelDir, "lexicon-gb-en.txt");

        var dictDir = Path.Combine(modelDir, "dict");
        if (Directory.Exists(dictDir))
        {
            config.Model.Kokoro.DictDir = dictDir;
        }

        config.Model.NumThreads = threads;
        config.Model.Provider = "cpu";
        config.Model.Debug = 0;
        return new OfflineTts(config);
    }
}
