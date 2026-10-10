namespace Helpers.Core.Input;

public enum PointerButton
{
    Left,
    Right,
    Middle,
    Other,
}

/// <summary>A point in screen pixels.</summary>
public readonly record struct GesturePoint(int X, int Y)
{
    public double DistanceTo(GesturePoint other)
    {
        var dx = (double)X - other.X;
        var dy = (double)Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

/// <summary>
/// Turns raw mouse and key events into "the user just selected text with the
/// mouse" and "the user did something else". A drag of a few pixels, a
/// double-click or a triple-click counts as a selection. Everything else,
/// including the mouse-down that starts a new gesture, dismisses the pill.
/// Platform-neutral and unit-tested; the hooks live in the Windows project.
/// </summary>
public sealed class SelectionGestureDetector
{
    private (GesturePoint Point, long Time)? _down;
    private GesturePoint? _lastUp;
    private long _lastUpTime;
    private int _clicks;

    /// <summary>How far the mouse must move between press and release to count as a drag.</summary>
    public int DragThresholdPixels { get; init; } = 6;

    /// <summary>Clicks closer together than this count as one multi-click.</summary>
    public int MultiClickMilliseconds { get; init; } = 500;

    public int MultiClickPixels { get; init; } = 5;

    /// <summary>Raised with the mouse-up point when a selection gesture completes.</summary>
    public event Action<GesturePoint>? SelectionMade;

    /// <summary>Raised on any input that should hide a pill already showing.</summary>
    public event Action? Dismissed;

    public void MouseDown(PointerButton button, GesturePoint point, long timeMs)
    {
        Dismissed?.Invoke();
        _down = button == PointerButton.Left ? (point, timeMs) : null;
    }

    public void MouseUp(PointerButton button, GesturePoint point, long timeMs)
    {
        if (button != PointerButton.Left || _down is not { } down)
        {
            return;
        }

        _down = null;
        if (down.Point.DistanceTo(point) >= DragThresholdPixels)
        {
            _clicks = 0;
            SelectionMade?.Invoke(point);
            return;
        }

        if (_lastUp is { } last && timeMs - _lastUpTime <= MultiClickMilliseconds && last.DistanceTo(point) <= MultiClickPixels)
        {
            _clicks++;
        }
        else
        {
            _clicks = 1;
        }

        _lastUp = point;
        _lastUpTime = timeMs;

        if (_clicks >= 2)
        {
            SelectionMade?.Invoke(point);
        }
    }

    public void Wheel() => Dismissed?.Invoke();

    public void KeyPressed() => Dismissed?.Invoke();

    /// <summary>Forgets a press in progress, for instance when it landed on one of our own windows.</summary>
    public void CancelPress() => _down = null;
}
