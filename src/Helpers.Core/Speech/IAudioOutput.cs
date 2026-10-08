namespace Helpers.Core.Speech;

/// <summary>
/// Plays clips in order through the system's default output device.
/// Platform projects implement it; Core only talks to this interface.
/// </summary>
public interface IAudioOutput : IDisposable
{
    bool IsPaused { get; }

    /// <summary>0 to 1. Applies to the open device at once and to every device opened later.</summary>
    float Volume { get; set; }

    /// <summary>The devices available right now, with the default first.</summary>
    IReadOnlyList<AudioDevice> ListDevices();

    /// <summary>Chooses a device by name. Null or the default id means the OS default. Takes effect from the next clip.</summary>
    void SelectDevice(string? name);

    /// <summary>Opens the device for the given sample rate. Calling it again with the same rate is a no-op.</summary>
    void Start(int sampleRate);

    /// <summary>
    /// Queues a clip and completes once it has been played to the end.
    /// Cancelling drops whatever has not yet been heard.
    /// </summary>
    Task PlayAsync(AudioClip clip, CancellationToken cancellationToken);

    void Pause();

    void Resume();

    /// <summary>Drops queued audio and closes the device.</summary>
    void Stop();
}
