using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Helpers.Core.Speech;

namespace Helpers.App.Windows;

/// <summary>Shown once, on the first launch: pick a voice, confirm the Read button and start-up. Done.</summary>
public partial class FirstRunWindow : Window
{
    private readonly IReadOnlyList<SpeechVoice> _voices;
    private readonly List<ToggleButton> _chips = [];
    private SpeechVoice _chosen;

    public FirstRunWindow(IReadOnlyList<SpeechVoice> voices, SpeechVoice current)
    {
        _voices = voices;
        _chosen = current;
        InitializeComponent();

        foreach (var voice in voices)
        {
            var chip = new ToggleButton
            {
                Classes = { "chip" },
                Content = voice.DisplayName,
                IsChecked = voice.Id == current.Id,
                Margin = new Thickness(0, 0, 8, 8),
                Tag = voice,
            };
            chip.Click += (_, _) => Choose(voice);
            _chips.Add(chip);
            VoiceChips.Children.Add(chip);
        }
    }

    /// <summary>Raised when a voice chip is picked, so it can be previewed straight away.</summary>
    public event Action<SpeechVoice>? VoiceChosen;

    public event Action<SpeechVoice>? PreviewRequested;

    /// <summary>Raised on Done with the choices made.</summary>
    public event Action<SpeechVoice, bool, bool>? Finished;

    private void Choose(SpeechVoice voice)
    {
        _chosen = voice;
        foreach (var chip in _chips)
        {
            chip.IsChecked = ReferenceEquals(chip.Tag, voice);
        }

        VoiceChosen?.Invoke(voice);
    }

    private void OnPreview(object? sender, RoutedEventArgs e) => PreviewRequested?.Invoke(_chosen);

    private void OnDone(object? sender, RoutedEventArgs e)
    {
        Finished?.Invoke(_chosen, ReadButtonSwitch.IsChecked == true, StartupSwitch.IsChecked == true);
        Close();
    }
}
