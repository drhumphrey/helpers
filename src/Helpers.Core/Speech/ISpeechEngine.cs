namespace Helpers.Core.Speech;

/// <summary>
/// A text-to-speech engine. Implementations own one model instance and
/// serialise synthesis calls themselves; callers may call from any thread.
/// </summary>
public interface ISpeechEngine
{
    bool IsLoaded { get; }

    /// <summary>The sample rate of every clip the engine produces.</summary>
    int SampleRate { get; }

    IReadOnlyList<SpeechVoice> Voices { get; }

    /// <summary>Downloads the model if needed, then loads it. Safe to call more than once.</summary>
    Task LoadAsync(IProgress<EngineProgress>? progress, CancellationToken cancellationToken);

    Task<AudioClip> SynthesiseAsync(SynthesisRequest request, CancellationToken cancellationToken);

    /// <summary>Frees the model's memory. The next call to <see cref="LoadAsync"/> loads it again.</summary>
    void Unload();
}
