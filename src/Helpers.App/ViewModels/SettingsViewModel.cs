using System.Collections.ObjectModel;
using Avalonia.Media;
using Helpers.App.Services;
using Helpers.Core.Settings;
using Helpers.Core.Speech;
using Helpers.Core.Text;

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
    private readonly Func<bool> _applyComposeHotkey;
    private readonly Action _applyReadableText;
    private readonly Func<bool, bool> _setStartWithWindows;

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
    private int _unloadAfterMinutes;
    private Choice<MessageScreen> _messagesOn;
    private bool _readButtonEnabled;
    private int _readButtonDelayMs;
    private string _excludedApps = string.Empty;
    private string _hotkeyText = string.Empty;
    private bool _hotkeyEnabled;
    private string _hotkeyStatus = string.Empty;
    private string _composeHotkeyText = string.Empty;
    private bool _composeHotkeyEnabled;
    private string _composeHotkeyStatus = string.Empty;
    private bool _watchClipboardOn;
    private Choice<ReadingFont> _readingFont;
    private double _readingFontSize;
    private double _readingLineSpacing;
    private Choice<ReadingTint> _readingTint;
    private Choice<CodeBlockReading> _codeBlocks;
    private Choice<FilePathReading> _filePaths;
    private bool _skipTables;
    private bool _sayLinks;
    private bool _sayEmails;
    private bool _sayNumbers;
    private string _newWord = string.Empty;
    private string _newSayAs = string.Empty;
    private bool _newCaseSensitive;
    private bool _startWithWindows;
    private string _startupStatus = string.Empty;

    public SettingsViewModel(
        SettingsStore store,
        ReadingController reading,
        Action<bool> watchClipboard,
        Action applyLook,
        Func<bool> applyHotkey,
        Func<bool> applyComposeHotkey,
        Action applyReadableText,
        Func<bool, bool> setStartWithWindows,
        bool startWithWindowsNow)
    {
        _store = store;
        _reading = reading;
        _watchClipboard = watchClipboard;
        _applyLook = applyLook;
        _applyHotkey = applyHotkey;
        _applyComposeHotkey = applyComposeHotkey;
        _applyReadableText = applyReadableText;
        _setStartWithWindows = setStartWithWindows;

        var settings = store.Current;

        Voices = reading.Voices;
        _voice = Voices.FirstOrDefault(v => v.Id == settings.VoiceId) ?? Voices[0];
        _speed = settings.Speed;
        _volume = settings.Volume;
        OutputDevices = reading.ListOutputDevices();
        _outputDevice = OutputDevices.FirstOrDefault(d => d.Name == settings.OutputDeviceName) ?? OutputDevices[0];
        _unloadAfterMinutes = settings.UnloadVoiceAfterMinutes;

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

        ReadingFonts =
        [
            new Choice<ReadingFont>(ReadingFont.Lexend, "Lexend"),
            new Choice<ReadingFont>(ReadingFont.AtkinsonHyperlegible, "Atkinson Hyperlegible"),
            new Choice<ReadingFont>(ReadingFont.System, "Windows default"),
        ];
        _readingFont = ReadingFonts.FirstOrDefault(f => f.Value == settings.ReadingFontChoice) ?? ReadingFonts[0];
        _readingFontSize = settings.ReadingFontSize;
        _readingLineSpacing = settings.ReadingLineSpacing;
        ReadingTints =
        [
            new Choice<ReadingTint>(ReadingTint.None, "None"),
            new Choice<ReadingTint>(ReadingTint.Cream, "Cream"),
            new Choice<ReadingTint>(ReadingTint.Grey, "Grey"),
        ];
        _readingTint = ReadingTints.First(t => t.Value == settings.ReadingTintChoice);

        var readingSettings = settings.Reading;
        CodeBlockChoices =
        [
            new Choice<CodeBlockReading>(CodeBlockReading.Skip, "Say \"code block, 12 lines\" and skip it"),
            new Choice<CodeBlockReading>(CodeBlockReading.FirstLine, "Say the count, then read the first line"),
            new Choice<CodeBlockReading>(CodeBlockReading.All, "Read every line"),
        ];
        _codeBlocks = CodeBlockChoices.First(c => c.Value == readingSettings.CodeBlocks);
        FilePathChoices =
        [
            new Choice<FilePathReading>(FilePathReading.FileNameOnly, "Say \"file\" and the file name"),
            new Choice<FilePathReading>(FilePathReading.JustSayFile, "Just say \"file\""),
            new Choice<FilePathReading>(FilePathReading.Full, "Read the whole path"),
        ];
        _filePaths = FilePathChoices.First(c => c.Value == readingSettings.FilePaths);
        _skipTables = readingSettings.SkipTables;
        _sayLinks = readingSettings.SayLinkForUrls;
        _sayEmails = readingSettings.SayEmailAddress;
        _sayNumbers = readingSettings.SayNumbersNaturally;
        Pronunciations = new ObservableCollection<PronunciationEntry>(readingSettings.Pronunciations.Entries);

        _hideAfterSeconds = settings.PlayerHideAfterSeconds;
        MessageScreens =
        [
            new Choice<MessageScreen>(MessageScreen.Primary, "The main screen"),
            new Choice<MessageScreen>(MessageScreen.Focused, "The screen I'm working on"),
        ];
        _messagesOn = MessageScreens.First(m => m.Value == settings.MessagesOn);
        _watchClipboardOn = settings.WatchClipboard;

        _readButtonEnabled = settings.ReadButtonEnabled;
        _readButtonDelayMs = settings.ReadButtonDelayMs;
        _excludedApps = string.Join(", ", settings.ExcludedApps);
        _hotkeyText = settings.ReadSelectionHotkey;
        _hotkeyEnabled = settings.ReadSelectionHotkeyEnabled;
        _composeHotkeyText = settings.ComposeHotkey;
        _composeHotkeyEnabled = settings.ComposeHotkeyEnabled;

        _startWithWindows = startWithWindowsNow;
    }

    public IReadOnlyList<SpeechVoice> Voices { get; }

    public IReadOnlyList<AudioDevice> OutputDevices { get; }

    public IReadOnlyList<Choice<Vibe>> Vibes { get; }

    public IReadOnlyList<Choice<ThemeChoice>> Themes { get; }

    public IReadOnlyList<Choice<double>> Scales { get; }

    public IReadOnlyList<Choice<string[]>> Palettes { get; }

    public IReadOnlyList<Choice<ReadingFont>> ReadingFonts { get; }

    public IReadOnlyList<Choice<ReadingTint>> ReadingTints { get; }

    public IReadOnlyList<Choice<CodeBlockReading>> CodeBlockChoices { get; }

    public IReadOnlyList<Choice<FilePathReading>> FilePathChoices { get; }

    public IReadOnlyList<Choice<MessageScreen>> MessageScreens { get; }

    public ObservableCollection<PronunciationEntry> Pronunciations { get; }

    public string SettingsFile => _store.FilePath;

    /// <summary>Set by the app so a change here reaches the toast window.</summary>
    public Action<bool>? MessagesFollowFocusChanged { get; set; }

    // Voice

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

    public int UnloadAfterMinutes
    {
        get => _unloadAfterMinutes;
        set
        {
            var clamped = Math.Clamp(value, 0, 240);
            if (Set(ref _unloadAfterMinutes, clamped))
            {
                _store.Update(s => s.UnloadVoiceAfterMinutes = clamped);
            }
        }
    }

    public void PreviewVoice() => _reading.Read("Hello, this is how I sound. Select some text anywhere and I will read it to you.");

    // Reading: readable text

    public Choice<ReadingFont> ReadingFontChoice
    {
        get => _readingFont;
        set
        {
            if (value is not null && Set(ref _readingFont, value))
            {
                _store.Update(s => s.ReadingFontChoice = value.Value);
                _applyReadableText();
            }
        }
    }

    public double ReadingFontSize
    {
        get => _readingFontSize;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 14, 32));
            if (Set(ref _readingFontSize, clamped))
            {
                _store.Update(s => s.ReadingFontSize = clamped);
                _applyReadableText();
            }
        }
    }

    public double ReadingLineSpacing
    {
        get => _readingLineSpacing;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 1.1, 2.2), 1);
            if (Set(ref _readingLineSpacing, clamped))
            {
                _store.Update(s => s.ReadingLineSpacing = clamped);
                _applyReadableText();
            }
        }
    }

    public Choice<ReadingTint> ReadingTintChoice
    {
        get => _readingTint;
        set
        {
            if (value is not null && Set(ref _readingTint, value))
            {
                _store.Update(s => s.ReadingTintChoice = value.Value);
                _applyReadableText();
            }
        }
    }

    // Reading: what gets said

    public Choice<CodeBlockReading> CodeBlocks
    {
        get => _codeBlocks;
        set
        {
            if (value is not null && Set(ref _codeBlocks, value))
            {
                _store.Update(s => s.Reading.CodeBlocks = value.Value);
            }
        }
    }

    public Choice<FilePathReading> FilePaths
    {
        get => _filePaths;
        set
        {
            if (value is not null && Set(ref _filePaths, value))
            {
                _store.Update(s => s.Reading.FilePaths = value.Value);
            }
        }
    }

    public bool SkipTables
    {
        get => _skipTables;
        set
        {
            if (Set(ref _skipTables, value))
            {
                _store.Update(s => s.Reading.SkipTables = value);
            }
        }
    }

    public bool SayLinks
    {
        get => _sayLinks;
        set
        {
            if (Set(ref _sayLinks, value))
            {
                _store.Update(s => s.Reading.SayLinkForUrls = value);
            }
        }
    }

    public bool SayEmails
    {
        get => _sayEmails;
        set
        {
            if (Set(ref _sayEmails, value))
            {
                _store.Update(s => s.Reading.SayEmailAddress = value);
            }
        }
    }

    public bool SayNumbers
    {
        get => _sayNumbers;
        set
        {
            if (Set(ref _sayNumbers, value))
            {
                _store.Update(s => s.Reading.SayNumbersNaturally = value);
            }
        }
    }

    // Reading: pronunciations

    public string NewWord
    {
        get => _newWord;
        set => Set(ref _newWord, value ?? string.Empty);
    }

    public string NewSayAs
    {
        get => _newSayAs;
        set => Set(ref _newSayAs, value ?? string.Empty);
    }

    public bool NewCaseSensitive
    {
        get => _newCaseSensitive;
        set => Set(ref _newCaseSensitive, value);
    }

    public void AddPronunciation()
    {
        var word = NewWord.Trim();
        var sayAs = NewSayAs.Trim();
        if (word.Length == 0 || sayAs.Length == 0)
        {
            return;
        }

        _store.Update(s => s.Reading.Pronunciations.Add(word, sayAs, NewCaseSensitive));
        var existing = Pronunciations.FirstOrDefault(p => string.Equals(p.Word, word, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            Pronunciations.Remove(existing);
        }

        Pronunciations.Add(new PronunciationEntry(word, sayAs, NewCaseSensitive));
        NewWord = string.Empty;
        NewSayAs = string.Empty;
        NewCaseSensitive = false;
    }

    public void RemovePronunciation(PronunciationEntry entry)
    {
        _store.Update(s => s.Reading.Pronunciations.Remove(entry.Word));
        Pronunciations.Remove(entry);
    }

    // Look

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

    // Player and messages

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

    public Choice<MessageScreen> MessagesOn
    {
        get => _messagesOn;
        set
        {
            if (value is not null && Set(ref _messagesOn, value))
            {
                _store.Update(s => s.MessagesOn = value.Value);
                MessagesFollowFocusChanged?.Invoke(value.Value == MessageScreen.Focused);
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

    // Read button and shortcut

    public bool ReadButtonEnabled
    {
        get => _readButtonEnabled;
        set
        {
            if (Set(ref _readButtonEnabled, value))
            {
                _store.Update(s => s.ReadButtonEnabled = value);
            }
        }
    }

    public int ReadButtonDelayMs
    {
        get => _readButtonDelayMs;
        set
        {
            var clamped = Math.Clamp(value, 0, 1000);
            if (Set(ref _readButtonDelayMs, clamped))
            {
                _store.Update(s => s.ReadButtonDelayMs = clamped);
            }
        }
    }

    public string ExcludedApps
    {
        get => _excludedApps;
        set
        {
            if (Set(ref _excludedApps, value ?? string.Empty))
            {
                var apps = _excludedApps
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(a => a.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? a[..^4] : a)
                    .Where(a => a.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _store.Update(s => s.ExcludedApps = apps);
            }
        }
    }

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

    public string ComposeHotkeyText
    {
        get => _composeHotkeyText;
        set
        {
            var gesture = Helpers.Core.Input.HotkeyGesture.Parse(value);
            if (gesture is null)
            {
                ComposeHotkeyStatus = "That isn't a shortcut. Try something like Ctrl+Alt+C.";
                return;
            }

            if (!gesture.HasModifier)
            {
                ComposeHotkeyStatus = "Add Ctrl, Alt, Shift or Win, or plain typing would trigger it.";
                return;
            }

            if (gesture.ToString() == _hotkeyText)
            {
                ComposeHotkeyStatus = "That's the Read the selection shortcut. Pick a different one.";
                return;
            }

            if (Set(ref _composeHotkeyText, gesture.ToString()))
            {
                _store.Update(s => s.ComposeHotkey = _composeHotkeyText);
                ComposeHotkeyStatus = _applyComposeHotkey() ? string.Empty : "Windows refused that shortcut. Another app may own it.";
            }
        }
    }

    public bool ComposeHotkeyEnabled
    {
        get => _composeHotkeyEnabled;
        set
        {
            if (Set(ref _composeHotkeyEnabled, value))
            {
                _store.Update(s => s.ComposeHotkeyEnabled = value);
                ComposeHotkeyStatus = _applyComposeHotkey() ? string.Empty : "Windows refused that shortcut. Another app may own it.";
            }
        }
    }

    public string ComposeHotkeyStatus
    {
        get => _composeHotkeyStatus;
        private set => Set(ref _composeHotkeyStatus, value);
    }

    // Start-up

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (Set(ref _startWithWindows, value))
            {
                var ok = _setStartWithWindows(value);
                _store.Update(s => s.StartWithWindows = value && ok);
                StartupStatus = ok ? string.Empty : "Windows wouldn't let the app change its start-up entry.";
            }
        }
    }

    public string StartupStatus
    {
        get => _startupStatus;
        private set => Set(ref _startupStatus, value);
    }

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
}
