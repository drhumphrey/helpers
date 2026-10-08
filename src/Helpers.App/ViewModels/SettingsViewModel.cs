using Avalonia.Media;
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
    private readonly Func<bool> _applyHotkey;
    private string _hotkeyText = string.Empty;
    private bool _hotkeyEnabled;
    private string _hotkeyStatus = string.Empty;

    private SpeechVoice _voice;
    private float _speed;
    private float _volume;
    private AudioDevice _outputDevice;
    private Choice<Vibe> _vibe;
    private Choice<ThemeChoice> _theme;
    private Choice<double> _scale;
    private Choice<string[]>? _palette;
    private Color _colour1;
    private Color _colour2;
    private Color _colour3;
    private bool _useThirdColour;
    private bool _loadingPalette;
    private bool _animate;
    private int _fleckDensity;
    private int _hideAfterSeconds;
    private bool _watchClipboardOn;

    public SettingsViewModel(SettingsStore store, ReadingController reading, Action<bool> watchClipboard, Action applyLook, Func<bool> applyHotkey)
    {
        _store = store;
        _reading = reading;
        _watchClipboard = watchClipboard;
        _applyLook = applyLook;
        _applyHotkey = applyHotkey;

        var settings = store.Current;
        _hotkeyText = settings.ReadSelectionHotkey;
        _hotkeyEnabled = settings.ReadSelectionHotkeyEnabled;
        Voices = reading.Voices;
        _voice = Voices.FirstOrDefault(v => v.Id == settings.VoiceId) ?? Voices[0];
        _speed = settings.Speed;
        _volume = settings.Volume;

        OutputDevices = reading.ListOutputDevices();
        _outputDevice = OutputDevices.FirstOrDefault(d => d.Name == settings.OutputDeviceName) ?? OutputDevices[0];

        Vibes =
        [
            new Choice<Vibe>(Vibe.Neon, "Neon"),
            new Choice<Vibe>(Vibe.Custom, "Custom colours"),
            new Choice<Vibe>(Vibe.Calm, "Calm"),
        ];
        _vibe = Vibes.FirstOrDefault(v => v.Value == settings.Vibe) ?? Vibes[0];

        Themes =
        [
            new Choice<ThemeChoice>(ThemeChoice.FollowOS, "Follow Windows"),
            new Choice<ThemeChoice>(ThemeChoice.Light, "Light"),
            new Choice<ThemeChoice>(ThemeChoice.Dark, "Dark"),
        ];
        _theme = Themes.First(t => t.Value == settings.Theme);

        Scales = UiScale.Choices.Select(s => new Choice<double>(s, $"{s:P0}")).ToList();
        _scale = Scales.OrderBy(s => Math.Abs(s.Value - settings.UiScale)).First();

        Palettes = Services.Vibes.Palettes.Select(p => new Choice<string[]>(p.Colours, p.Name)).ToList();
        var colours = Services.Vibes.ParseColours(settings.GradientColours);
        _colour1 = colours[0];
        _colour2 = colours[1];
        _useThirdColour = colours.Length > 2;
        _colour3 = colours.Length > 2 ? colours[2] : Color.Parse("#FFE14D");

        _animate = settings.AnimateWhileReading;
        _fleckDensity = settings.FleckDensity;
        _hideAfterSeconds = settings.PlayerHideAfterSeconds;
        _watchClipboardOn = settings.WatchClipboard;
    }

    public IReadOnlyList<SpeechVoice> Voices { get; }

    public IReadOnlyList<AudioDevice> OutputDevices { get; }

    public IReadOnlyList<Choice<Vibe>> Vibes { get; }

    public IReadOnlyList<Choice<ThemeChoice>> Themes { get; }

    public IReadOnlyList<Choice<double>> Scales { get; }

    public IReadOnlyList<Choice<string[]>> Palettes { get; }

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
                Raise(nameof(IsCustom));
                Raise(nameof(IsGradientVibe));
                _applyLook();
            }
        }
    }

    public bool IsCalm => _vibe.Value == Vibe.Calm;

    public bool IsCustom => _vibe.Value == Vibe.Custom;

    public bool IsGradientVibe => _vibe.Value != Vibe.Calm;

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

    public Choice<double> ScaleChoice
    {
        get => _scale;
        set
        {
            if (value is not null && Set(ref _scale, value))
            {
                _store.Update(s => s.UiScale = value.Value);
                UiScale.Set(value.Value);
            }
        }
    }

    public Choice<string[]>? Palette
    {
        get => _palette;
        set
        {
            if (value is not null && Set(ref _palette, value))
            {
                var colours = Services.Vibes.ParseColours(value.Value);
                _loadingPalette = true;
                try
                {
                    Colour1 = colours[0];
                    Colour2 = colours[1];
                    UseThirdColour = colours.Length > 2;
                    if (colours.Length > 2)
                    {
                        Colour3 = colours[2];
                    }
                }
                finally
                {
                    _loadingPalette = false;
                }

                ApplyColours();
            }
        }
    }

    public Color Colour1
    {
        get => _colour1;
        set
        {
            if (Set(ref _colour1, value))
            {
                ApplyColours();
            }
        }
    }

    public Color Colour2
    {
        get => _colour2;
        set
        {
            if (Set(ref _colour2, value))
            {
                ApplyColours();
            }
        }
    }

    public Color Colour3
    {
        get => _colour3;
        set
        {
            if (Set(ref _colour3, value))
            {
                ApplyColours();
            }
        }
    }

    public bool UseThirdColour
    {
        get => _useThirdColour;
        set
        {
            if (Set(ref _useThirdColour, value))
            {
                ApplyColours();
            }
        }
    }

    /// <summary>Saves the colours and re-skins the app at once. Every picker change comes through here.</summary>
    private void ApplyColours()
    {
        if (_loadingPalette)
        {
            return;
        }

        var colours = new List<string> { Hex(_colour1), Hex(_colour2) };
        if (_useThirdColour)
        {
            colours.Add(Hex(_colour3));
        }

        _store.Update(s => s.GradientColours = colours);
        if (IsCustom)
        {
            _applyLook();
        }
    }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public bool AnimateWhileReading
    {
        get => _animate;
        set
        {
            if (Set(ref _animate, value))
            {
                _store.Update(s => s.AnimateWhileReading = value);
                _reading.SetAnimate(value);
            }
        }
    }

    public int FleckDensity
    {
        get => _fleckDensity;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (Set(ref _fleckDensity, clamped))
            {
                _store.Update(s => s.FleckDensity = clamped);
                _reading.SetFleckDensity(clamped);
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

    /// <summary>The shortcut as text, such as "Ctrl+Alt+Space". Set from the key-capture box.</summary>
    public string HotkeyText
    {
        get => _hotkeyText;
        set
        {
            var gesture = Helpers.Core.Input.HotkeyGesture.Parse(value);
            if (gesture is null)
            {
                HotkeyStatus = "That isn't a shortcut. Try something like Ctrl+Alt+Space.";
                return;
            }

            if (!gesture.HasModifier)
            {
                HotkeyStatus = "Add Ctrl, Alt, Shift or Win, or plain typing would trigger it.";
                return;
            }

            if (Set(ref _hotkeyText, gesture.ToString()))
            {
                _store.Update(s => s.ReadSelectionHotkey = _hotkeyText);
                HotkeyStatus = _applyHotkey() ? string.Empty : "Windows refused that shortcut. Another app may own it.";
            }
        }
    }

    public bool HotkeyEnabled
    {
        get => _hotkeyEnabled;
        set
        {
            if (Set(ref _hotkeyEnabled, value))
            {
                _store.Update(s => s.ReadSelectionHotkeyEnabled = value);
                HotkeyStatus = _applyHotkey() ? string.Empty : "Windows refused that shortcut. Another app may own it.";
            }
        }
    }

    public string HotkeyStatus
    {
        get => _hotkeyStatus;
        private set => Set(ref _hotkeyStatus, value);
    }
}
