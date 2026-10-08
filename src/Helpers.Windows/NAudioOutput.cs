using Helpers.Core.Speech;
using NAudio.Wave;

namespace Helpers.Windows;

/// <summary>
/// Plays clips through a Windows output device with NAudio.
/// One streaming buffer; a clip is "played" once the buffer has drained.
/// </summary>
public sealed class NAudioOutput : IAudioOutput
{
    private const int LatencyMs = 120;

    private readonly object _sync = new();
    private WaveOut? _device;
    private BufferedWaveProvider? _buffer;
    private int _sampleRate;
    private float _volume = 1.0f;
    private string? _deviceName;

    public bool IsPaused { get; private set; }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            lock (_sync)
            {
                if (_device is not null)
                {
                    _device.Volume = _volume;
                }
            }
        }
    }

    public IReadOnlyList<AudioDevice> ListDevices()
    {
        var devices = new List<AudioDevice> { AudioDevice.Default };
        for (var i = 0; i < WaveOut.DeviceCount; i++)
        {
            var name = WaveOut.GetCapabilities(i).ProductName;
            if (!string.IsNullOrWhiteSpace(name))
            {
                devices.Add(new AudioDevice(i.ToString(), name));
            }
        }

        return devices;
    }

    public void SelectDevice(string? name)
    {
        lock (_sync)
        {
            var wanted = string.IsNullOrWhiteSpace(name) || name == AudioDevice.DefaultId ? null : name;
            if (wanted == _deviceName)
            {
                return;
            }

            _deviceName = wanted;

            // Close the current device; the next clip opens the new one.
            StopCore();
        }
    }

    public void Start(int sampleRate)
    {
        lock (_sync)
        {
            if (_device is not null && _sampleRate == sampleRate)
            {
                return;
            }

            StopCore();
            _sampleRate = sampleRate;
            _buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1), TimeSpan.FromMinutes(5))
            {
                DiscardOnBufferOverflow = false,
                ReadFully = true,
            };

            _device = new WaveOut
            {
                DeviceNumber = ResolveDeviceNumber(),
                BufferMilliseconds = LatencyMs / 2,
                NumberOfBuffers = 2,
                Volume = _volume,
            };
            _device.Init(_buffer);
            _device.Play();
            IsPaused = false;
        }
    }

    public async Task PlayAsync(AudioClip clip, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (clip.Samples.Length == 0)
        {
            return;
        }

        BufferedWaveProvider buffer;
        lock (_sync)
        {
            if (_buffer is null || _device is null || _sampleRate != clip.SampleRate)
            {
                Start(clip.SampleRate);
            }

            buffer = _buffer!;
        }

        var bytes = new byte[clip.Samples.Length * sizeof(float)];
        Buffer.BlockCopy(clip.Samples, 0, bytes, 0, bytes.Length);

        using var dropOnCancel = cancellationToken.Register(() => buffer.ClearBuffer());
        buffer.AddSamples(bytes, 0, bytes.Length);

        while (buffer.BufferedBytes > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }

        // The device still holds about one latency window after the buffer empties.
        await Task.Delay(LatencyMs, cancellationToken).ConfigureAwait(false);
    }

    public void Pause()
    {
        lock (_sync)
        {
            IsPaused = true;
            _device?.Pause();
        }
    }

    public void Resume()
    {
        lock (_sync)
        {
            IsPaused = false;
            _device?.Play();
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            StopCore();
        }
    }

    public void Dispose() => Stop();

    private int ResolveDeviceNumber()
    {
        if (_deviceName is null)
        {
            return -1;
        }

        for (var i = 0; i < WaveOut.DeviceCount; i++)
        {
            if (string.Equals(WaveOut.GetCapabilities(i).ProductName, _deviceName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void StopCore()
    {
        _buffer?.ClearBuffer();
        _device?.Stop();
        _device?.Dispose();
        _device = null;
        _buffer = null;
        IsPaused = false;
    }
}
