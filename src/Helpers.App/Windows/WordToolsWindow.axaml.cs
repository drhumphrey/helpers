using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Helpers.App.Services;
using Helpers.Core.Words;

namespace Helpers.App.Windows;

/// <summary>
/// One word, in pieces: its syllables, how to say it, what it means, and
/// other words for it. Opened from the right-click menu in Compose.
/// </summary>
public partial class WordToolsWindow : ShellWindow
{
    private readonly WordToolsService _service;
    private WordInfo? _info;

    public WordToolsWindow(WordToolsService service)
    {
        _service = service;
        InitializeComponent();
    }

    /// <summary>Raised for Hear it (slowly is false) and Slowly (true).</summary>
    public event Action<string, bool>? HearRequested;

    /// <summary>Raised when a synonym chip is pressed.</summary>
    public event Action<string>? ReplaceRequested;

    protected override void RequestClose() => Hide();

    public void Show(WordInfo info, bool dictionaryAvailable, bool pronunciationAvailable, PixelPoint? near)
    {
        _info = info;
        WordText.Text = info.Word;
        SyllableText.Text = info.Syllables.Count > 1 ? info.SyllableText : string.Empty;
        SyllableText.IsVisible = info.Syllables.Count > 1;

        RespellingText.Text = info.Respelling ?? string.Empty;
        RespellingText.IsVisible = info.HasPronunciation;
        IpaText.Text = info.Ipa is { } ipa ? $"/{ipa}/" : string.Empty;
        IpaText.IsVisible = info.HasPronunciation;
        NoSoundText.IsVisible = !info.HasPronunciation;

        DownloadPanel.IsVisible = !dictionaryAvailable;
        MeaningsList.ItemsSource = info.Meanings;
        NoMeaningText.IsVisible = dictionaryAvailable && !info.HasMeanings;

        SynonymChips.Children.Clear();
        var synonyms = info.AllSynonyms;
        foreach (var synonym in synonyms)
        {
            var chip = new Button { Classes = { "chip" }, Content = synonym };
            var word = synonym;
            chip.Click += (_, _) => ReplaceRequested?.Invoke(word);
            SynonymChips.Children.Add(chip);
        }

        SynonymLabel.IsVisible = synonyms.Count > 0;
        SynonymHint.IsVisible = synonyms.Count > 0;

        if (!IsVisible)
        {
            if (near is { } point && Screens.ScreenFromPoint(point) is { } screen)
            {
                var size = PixelSize.FromSize(new Size(Width, Height), screen.Scaling);
                var area = screen.WorkingArea;
                Position = new PixelPoint(
                    Math.Clamp(point.X + 24, area.X, Math.Max(area.X, area.Right - size.Width)),
                    Math.Clamp(point.Y - size.Height / 2, area.Y, Math.Max(area.Y, area.Bottom - size.Height)));
            }

            base.Show();
        }

        Activate();
    }

    private void OnHear(object? sender, RoutedEventArgs e)
    {
        if (_info is not null)
        {
            HearRequested?.Invoke(_info.Word, false);
        }
    }

    private void OnHearSlowly(object? sender, RoutedEventArgs e)
    {
        if (_info is not null)
        {
            HearRequested?.Invoke(_info.Word, true);
        }
    }

    private void OnGoogle(object? sender, RoutedEventArgs e)
    {
        if (_info is not null)
        {
            Links.Open(Links.GoogleDefine(_info.Word));
        }
    }

    private void OnWiktionary(object? sender, RoutedEventArgs e)
    {
        if (_info is not null)
        {
            Links.Open(Links.Wiktionary(_info.Word));
        }
    }

    private async void OnDownload(object? sender, RoutedEventArgs e)
    {
        DownloadButton.IsEnabled = false;
        DownloadProgress.IsVisible = true;
        DownloadStatus.Text = "Downloading…";
        try
        {
            await _service.DownloadDictionaryAsync(new Progress<double>(fraction => DownloadProgress.Value = fraction * 100));
            DownloadStatus.Text = string.Empty;
            if (_info is not null)
            {
                _service.Show(_info.Word);
            }
        }
        catch (Exception ex)
        {
            DownloadStatus.Text = $"Couldn't download it: {ex.Message}";
            DownloadButton.IsEnabled = true;
        }
        finally
        {
            DownloadProgress.IsVisible = false;
        }
    }
}
