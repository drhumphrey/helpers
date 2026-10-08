namespace Helpers.Core.Speech;

/// <summary>A voice the engine can speak with.</summary>
/// <param name="Id">Stable id stored in settings, such as "bm_george".</param>
/// <param name="DisplayName">What the user sees, such as "George".</param>
/// <param name="SpeakerId">The engine's own number for the voice.</param>
public sealed record SpeechVoice(string Id, string DisplayName, int SpeakerId);

/// <summary>One piece of synthesised audio: mono float samples at a sample rate.</summary>
public sealed class AudioClip
{
    public AudioClip(float[] samples, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        Samples = samples;
        SampleRate = sampleRate;
    }

    public float[] Samples { get; }

    public int SampleRate { get; }

    public TimeSpan Duration => TimeSpan.FromSeconds(Samples.Length / (double)SampleRate);
}

/// <summary>What to say and how.</summary>
public sealed record SynthesisRequest(string Text, SpeechVoice Voice, float Speed);

/// <summary>Progress while the engine downloads or loads its model.</summary>
/// <param name="Stage">Short text for the user, such as "Downloading the voice".</param>
/// <param name="Fraction">0 to 1 when known, otherwise null.</param>
public readonly record struct EngineProgress(string Stage, double? Fraction);
