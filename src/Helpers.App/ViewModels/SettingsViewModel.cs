using Helpers.App.Services;
using Helpers.Core.Settings;
using Helpers.Core.Speech;

namespace Helpers.App.ViewModels;

/// <summary>A named choice for a combo box.</summary>
public sealed record Choice<T>(T Value, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Binds the Settings window to the settings file. Every change saves at once
/// and is applied live through the services it was given.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private readonly ReadingController _reading;
    private readonly Action<bool> _watchClipboard;
    private readonly Action _applyLook;

    private SpeechVoice _voice;
    private float _speed;
    private float _volume;
    private AudioDevice _outputDevice;
    private Choice<Vibe> _vibe;
    private Choice<ThemeChoice> _theme;
    private int _hideAfterSeconds;
    private bool _watchClipboardOn;

    public SettingsViewModel(SettingsStore store, ReadingController reading, Action<bool> watchClipboard, Action applyLook)
    {
        _store = store;
        _reading = reading;
        _watchClipboard = watchClipboard;
        _applyLook = applyLook;

        var settings = store.Current;
        Voices = reading.Voices;
        _voice = Voices.FirstOrDefault(v => v.Id == settings.VoiceId) ?? Voices[0];
        _speed = settings.Speed;
        _volume = settings.Volume;

        OutputDevices = reading.ListOutputDevices();
        _outputDevice = OutputDevices.FirstOrDefault(d => d.Name == settings.OutputDeviceName) ?? OutputDevices[0];

        Vibes =
        [
            new Choice<Vibe>(Vibe.Neon, "Neon"),
            new Choice<Vibe>(Vibe.Calm, "Calm"),
        ];
        _vibe = Vibes.First(v => v.Value == (settings.Vibe == Vibe.Calm ? Vibe.Calm : Vibe.Neon));

        Themes =
        [
            new Choice<ThemeChoice>(ThemeChoice.FollowOS, "Follow Windows"),
            new Choice<ThemeChoice>(ThemeChoice.Light, "Light"),
            new Choice<ThemeChoice>(ThemeChoice.Dark, "Dark"),
        ];
        _theme = Themes.First(t => t.Value == settings.Theme);

        _hideAfterSeconds = settings.PlayerHideAfterSeconds;
        _watchClipboardOn = settings.WatchClipboard;
    }

    public IReadOnlyList<SpeechVoice> Voices { get; }

    public IReadOnlyList<AudioDevice> OutputDevices { get; }

    public IReadOnlyList<Choice<Vibe>> Vibes { get; }

    public IReadOnlyList<Choice<ThemeChoice>> Themes { get; }

    public string SettingsFile => _store.FilePath;

    public SpeechVoice Voice
    {
        get => _voice;
        set
        {
            if (value is not null && Set(ref _voice, value))
            {
                _store.Update(s => s.VoiceId = value.Id);
                _reading.SetVoice(value);
            }
        }
    }

    public float Speed
    {
        get => _speed;
        set
        {
            var rounded = MathF.Round(Math.Clamp(value, 0.5f, 2.0f), 1);
            if (Set(ref _speed, rounded))
            {
                Raise(nameof(SpeedText));
                _store.Update(s => s.Speed = rounded);
                _reading.SetSpeed(rounded);
            }
        }
    }

    public string SpeedText => $"{Speed:0.0}×";

    /// <summary>0 to 100 for the slider.</summary>
    public float VolumePercent
    {
        get => _volume * 100f;
        set
        {
            var fraction = Math.Clamp(MathF.Round(value) / 100f, 0f, 1f);
            if (Set(ref _volume, fraction))
            {
                Raise(nameof(VolumeText));
                _store.Update(s => s.Volume = fraction);
                _reading.SetVolume(fraction);
            }
        }
    }

    public string VolumeText => $"{_volume:P0}";

    public AudioDevice OutputDevice
    {
        get => _outputDevice;
        set
        {
            if (value is not null && Set(ref _outputDevice, value))
            {
                var name = value.IsDefault ? null : value.Name;
                _store.Update(s => s.OutputDeviceName = name);
                _reading.SelectOutputDevice(name);
            }
        }
    }

    public Choice<Vibe> VibeChoice
    {
        get => _vibe;
        set
        {
            if (value is not null && Set(ref _vibe, value))
            {
                _store.Update(s => s.Vibe = value.Value);
                Raise(nameof(IsCalm));
                _applyLook();
            }
        }
    }

    public bool IsCalm => _vibe.Value == Vibe.Calm;

    public Choice<ThemeChoice> ThemeChoiceValue
    {
        get => _theme;
        set
        {
            if (value is not null && Set(ref _theme, value))
            {
                _store.Update(s => s.Theme = value.Value);
                _applyLook();
            }
        }
    }

    public int HideAfterSeconds
    {
        get => _hideAfterSeconds;
        set
        {
            var clamped = Math.Clamp(value, 0, 60);
            if (Set(ref _hideAfterSeconds, clamped))
            {
                _store.Update(s => s.PlayerHideAfterSeconds = clamped);
            }
        }
    }

    public bool WatchClipboardOn
    {
        get => _watchClipboardOn;
        set
        {
            if (Set(ref _watchClipboardOn, value))
            {
                _store.Update(s => s.WatchClipboard = value);
                _watchClipboard(value);
            }
        }
    }

    public void PreviewVoice() => _reading.Read("Hello, this is how I sound. Select some text anywhere and I will read it to you.");
}
