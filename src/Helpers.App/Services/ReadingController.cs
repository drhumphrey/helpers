using Avalonia;
using Avalonia.Threading;
using Helpers.App.Overlays;
using Helpers.App.ViewModels;
using Helpers.Core.Settings;
using Helpers.Core.Speech;
using Helpers.Core.Text;

namespace Helpers.App.Services;

/// <summary>
/// Owns the engine, the audio output and the one reading at a time. Turns
/// text into a session, shows the player, and reports problems as toasts.
/// </summary>
public sealed class ReadingController : IDisposable
{
    private readonly ISpeechEngine _engine;
    private readonly IAudioOutput _output;
    private readonly SettingsStore _settings;
    private readonly ToastService _toasts;
    private readonly PlayerViewModel _player;
    private PlayerWindow? _window;
    private ReadingSession? _session;
    private DispatcherTimer? _hideTimer;

    public ReadingController(ISpeechEngine engine, IAudioOutput output, SettingsStore settings, ToastService toasts)
    {
        _engine = engine;
        _output = output;
        _settings = settings;
        _toasts = toasts;

        _player = new PlayerViewModel(engine.Voices)
        {
            Speed = settings.Current.Speed,
            Voice = engine.Voices.FirstOrDefault(v => v.Id == settings.Current.VoiceId) ?? engine.Voices[0],
        };
        _output.Volume = settings.Current.Volume;
        _output.SelectDevice(settings.Current.OutputDeviceName);

        _player.SpeedChanged += speed => _settings.Update(s => s.Speed = speed);
        _player.VoiceChanged += voice => _settings.Update(s => s.VoiceId = voice.Id);
        _player.RestartRequested += index =>
        {
            if (_lastSegments is not null)
            {
                StartSession(_lastSegments, index);
            }
        };
    }

    private IReadOnlyList<SpeechSegment>? _lastSegments;

    public bool IsReading => _session is { State: ReadingState.Playing or ReadingState.Paused or ReadingState.Loading };

    /// <summary>Raised by the player's cog button.</summary>
    public event Action? SettingsRequested;

    public IReadOnlyList<SpeechVoice> Voices => _engine.Voices;

    public IReadOnlyList<AudioDevice> ListOutputDevices() => _output.ListDevices();

    public void SelectOutputDevice(string? name) => _output.SelectDevice(name);

    public void SetVolume(float volume) => _output.Volume = volume;

    public void SetSpeed(float speed) => _player.Speed = speed;

    public void SetVoice(SpeechVoice voice) => _player.Voice = voice;

    public void SetAnimate(bool on)
    {
        if (_window is not null)
        {
            _window.AnimateWhileReading = on;
        }
    }

    /// <summary>Loads the voice in the background, with a progress toast if a download is needed.</summary>
    public async Task WarmUpAsync()
    {
        ToastItem? progress = null;
        var reporter = new Progress<EngineProgress>(p => Dispatcher.UIThread.Post(() =>
        {
            if (p.Stage.StartsWith("Downloading", StringComparison.Ordinal))
            {
                progress ??= _toasts.Progress(p.Stage);
                progress.Fraction = p.Fraction ?? 0;
                progress.Text = p.Fraction is { } f ? $"{p.Stage}, {f:P0}" : p.Stage;
                progress.Detail = "About 350 MB, one time only.";
            }
            else if (progress is not null)
            {
                progress.Text = p.Stage;
                progress.Detail = string.Empty;
                if (p.Fraction >= 1)
                {
                    _toasts.Dismiss(progress);
                    progress = null;
                }
            }
        }));

        try
        {
            await _engine.LoadAsync(reporter, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (progress is not null)
            {
                _toasts.Dismiss(progress);
            }

            _toasts.Error($"Couldn't get the voice ready: {ex.Message}");
        }
    }

    public void ReadClipboard()
    {
        var text = Helpers.Windows.ClipboardText.TryGet();
        if (string.IsNullOrWhiteSpace(text))
        {
            _toasts.Info("Nothing on the clipboard to read");
            return;
        }

        Read(text);
    }

    public void Read(string text)
    {
        var segments = ReadingPipeline.Prepare(text, _settings.Current.Reading);
        if (segments.Count == 0)
        {
            _toasts.Info("Nothing to read");
            return;
        }

        _lastSegments = segments;
        StartSession(segments, 0);
    }

    private void StartSession(IReadOnlyList<SpeechSegment> segments, int startIndex)
    {
        StopCurrent();

        var session = new ReadingSession(_engine, _output, segments, _player.Voice ?? _engine.Voices[0], _player.Speed, startIndex);
        _session = session;
        _player.Attach(session);
        session.StateChanged += state => Dispatcher.UIThread.Post(() => OnStateChanged(session, state));
        session.Failed += message => _toasts.Error($"Couldn't read that: {message}");

        ShowPlayer();
        _ = session.RunAsync();
    }

    public void TogglePause() => _session?.TogglePause();

    public void Stop() => StopCurrent();

    public void ShowPlayer()
    {
        _hideTimer?.Stop();
        if (_window is null)
        {
            var current = _settings.Current;
            _window = new PlayerWindow(_player)
            {
                AnimateWhileReading = current.AnimateWhileReading,
                ExpandedSize = current.ReadingViewWidth is { } w && current.ReadingViewHeight is { } h ? new Size(w, h) : null,
            };
            _window.Moved += point => _settings.Update(s => s.PlayerPlacement = new WindowPlacement(point.X, point.Y));
            _window.ReadingViewResized += (w, h) => _settings.Update(s =>
            {
                s.ReadingViewWidth = w;
                s.ReadingViewHeight = h;
            });
            _window.SettingsRequested += () => SettingsRequested?.Invoke();
            _window.Closed += (_, _) => _window = null;
            _window.Show();
            var remembered = _settings.Current.PlayerPlacement;
            _window.PlaceAt(remembered is null ? null : new PixelPoint(remembered.X, remembered.Y));
        }
        else if (!_window.IsVisible)
        {
            _window.Show();
        }
    }

    public void Dispose()
    {
        StopCurrent();
        _output.Dispose();
    }

    private void OnStateChanged(ReadingSession session, ReadingState state)
    {
        if (session != _session)
        {
            return;
        }

        if (state is ReadingState.Finished or ReadingState.Stopped or ReadingState.Failed)
        {
            var seconds = _settings.Current.PlayerHideAfterSeconds;
            if (seconds > 0 && !_player.IsExpanded)
            {
                _hideTimer?.Stop();
                _hideTimer = new DispatcherTimer(TimeSpan.FromSeconds(seconds), DispatcherPriority.Background, (_, _) =>
                {
                    _hideTimer?.Stop();
                    if (!IsReading)
                    {
                        _window?.Hide();
                    }
                });
                _hideTimer.Start();
            }
        }
    }

    private void StopCurrent()
    {
        _hideTimer?.Stop();
        if (_session is null)
        {
            return;
        }

        _player.Detach();
        _session.Stop();
        _session.Dispose();
        _session = null;
    }
}
