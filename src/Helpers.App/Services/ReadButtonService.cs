using Avalonia;
using Avalonia.Threading;
using Helpers.App.Overlays;
using Helpers.Core.Capture;
using Helpers.Core.Input;
using Helpers.Core.Settings;
using Helpers.Windows;

namespace Helpers.App.Services;

/// <summary>
/// Shows the Read button after a mouse selection, following the brief's rules:
/// not on frames or drags between windows, not over our own windows, not in
/// excluded apps, not while paused. Hides on any other input or after a while.
/// </summary>
public sealed class ReadButtonService : IDisposable
{
    private readonly SelectionGestureDetector _detector = new();
    private readonly InputMonitor _input;
    private readonly SettingsStore _settings;
    private readonly ReadingController _reading;
    private readonly ISelectionSource _selection;
    private readonly ToastService _toasts;
    private readonly DispatcherTimer _showTimer;
    private PillWindow? _pill;
    private nint _pressedWindow;
    private GesturePoint _pressedPoint;
    private GesturePoint _pendingPoint;
    private DateTime? _pausedUntil;

    public ReadButtonService(InputMonitor input, SettingsStore settings, ReadingController reading, ISelectionSource selection, ToastService toasts)
    {
        _input = input;
        _settings = settings;
        _reading = reading;
        _selection = selection;
        _toasts = toasts;

        _showTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(settings.Current.ReadButtonDelayMs), DispatcherPriority.Background, (_, _) => ShowPill());
        _detector.SelectionMade += OnSelectionMade;
        _detector.Dismissed += () =>
        {
            _showTimer.Stop();
            _pill?.HidePill();
        };

        input.MouseDown += OnMouseDown;
        input.MouseUp += OnMouseUp;
        input.Wheel += () => _detector.Wheel();
        input.KeyPressed += () => _detector.KeyPressed();
    }

    public bool IsPaused => _pausedUntil is { } until && until > DateTime.UtcNow;

    /// <summary>Stops the button appearing for a while, for screen sharing or presenting.</summary>
    public void PauseFor(TimeSpan span)
    {
        _pausedUntil = DateTime.UtcNow + span;
        _pill?.HidePill();
    }

    public void Resume() => _pausedUntil = null;

    public void Dispose()
    {
        _showTimer.Stop();
        _pill?.Close();
    }

    private void OnMouseDown(PointerButton button, GesturePoint point, long time, bool ours)
    {
        if (ours)
        {
            // A click on the pill or the player must neither hide the pill nor start a gesture.
            _detector.CancelPress();
            return;
        }

        _pressedWindow = WindowAtPoint.TopLevelHandle(point.X, point.Y);
        _pressedPoint = point;
        _detector.MouseDown(button, point, time);
    }

    private void OnMouseUp(PointerButton button, GesturePoint point, long time, bool ours)
    {
        if (ours)
        {
            return;
        }

        _detector.MouseUp(button, point, time);
    }

    private void OnSelectionMade(GesturePoint point)
    {
        if (!_settings.Current.ReadButtonEnabled || IsPaused)
        {
            return;
        }

        // Released over a different window than pressed: a window or file drag, not a selection.
        var releasedWindow = WindowAtPoint.TopLevelHandle(point.X, point.Y);
        if (releasedWindow == 0 || releasedWindow != _pressedWindow)
        {
            return;
        }

        // Pressed on a title bar, border or scrollbar: a drag, not a selection.
        if (WindowAtPoint.IsOnFrame(WindowAtPoint.Handle(_pressedPoint.X, _pressedPoint.Y), _pressedPoint.X, _pressedPoint.Y))
        {
            return;
        }

        var process = WindowAtPoint.ProcessName(releasedWindow);
        if (_settings.Current.ExcludedApps.Any(app => string.Equals(app, process, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _pendingPoint = point;
        _showTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(0, _settings.Current.ReadButtonDelayMs));
        _showTimer.Stop();
        _showTimer.Start();
    }

    private void ShowPill()
    {
        _showTimer.Stop();
        _pill ??= CreatePill();
        _pill.ShowAt(new PixelPoint(_pendingPoint.X, _pendingPoint.Y), TimeSpan.FromSeconds(3));
    }

    private PillWindow CreatePill()
    {
        var pill = new PillWindow();
        pill.ReadRequested += () => _ = ReadAsync();
        pill.Closed += (_, _) => _pill = null;
        return pill;
    }

    private async Task ReadAsync()
    {
        try
        {
            await _reading.ReadSelectionOrStopAsync(_selection);
        }
        catch (Exception ex)
        {
            _toasts.Error($"Couldn't read that: {ex.Message}");
        }
    }
}
