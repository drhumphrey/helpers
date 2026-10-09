using Avalonia;
using Avalonia.Controls;
using Helpers.App.Windows;
using Helpers.Core.Settings;
using Helpers.Core.Words;

namespace Helpers.App.Services;

/// <summary>
/// The word tools: syllables, how to say it, meanings and synonyms for one
/// word, in a small window. Owns the dictionary download and the window.
/// </summary>
public sealed class WordToolsService : IDisposable
{
    private readonly SettingsStore _settings;
    private readonly ReadingController _reading;
    private readonly ToastService _toasts;
    private readonly WordLookup _lookup;
    private readonly WordNet _wordNet;
    private WordToolsWindow? _window;
    private CancellationTokenSource? _download;

    public WordToolsService(SettingsStore settings, ReadingController reading, ToastService toasts)
    {
        _settings = settings;
        _reading = reading;
        _toasts = toasts;

        var modelsRoot = settings.Current.ModelsFolder ?? SettingsStore.DefaultModelsFolder();
        var lexicon = Path.Combine(modelsRoot, "kokoro-multi-lang-v1_0", "lexicon-gb-en.txt");
        _wordNet = new WordNet(WordNetDownload.FolderUnder(modelsRoot));
        _lookup = new WordLookup(Hyphenator.British, new Pronunciation(lexicon), _wordNet);
    }

    /// <summary>Raised when the user picks a synonym to use in place of the word looked up.</summary>
    public event Action<string>? ReplaceRequested;

    public bool DictionaryAvailable => _wordNet.IsAvailable;

    public bool IsDownloading => _download is not null;

    /// <summary>Shows the tools for a word, near a point on screen if given.</summary>
    public void Show(string word, PixelPoint? near = null)
    {
        var info = _lookup.Lookup(word);
        if (info.Word.Length == 0)
        {
            return;
        }

        var window = EnsureWindow();
        window.Show(info, _lookup.DictionaryAvailable, _lookup.PronunciationAvailable, near);
    }

    public async Task DownloadDictionaryAsync(IProgress<double> progress)
    {
        if (_download is not null)
        {
            return;
        }

        _download = new CancellationTokenSource();
        try
        {
            await WordNetDownload.DownloadAsync(_wordNet.Folder, progress, _download.Token);
        }
        finally
        {
            _download.Dispose();
            _download = null;
        }
    }

    public void CancelDownload() => _download?.Cancel();

    public void Dispose()
    {
        _download?.Cancel();
        _window?.Close();
        _window = null;
    }

    private WordToolsWindow EnsureWindow()
    {
        if (_window is not null)
        {
            return _window;
        }

        var window = new WordToolsWindow(this);
        window.HearRequested += (text, slowly) => _reading.Read(text, slowly ? 0.6f : null);
        window.ReplaceRequested += word => ReplaceRequested?.Invoke(word);
        window.Closed += (_, _) => _window = null;
        _window = window;
        return window;
    }
}
