using Avalonia.Threading;
using Helpers.Core.Input;
using Helpers.Windows;

namespace Helpers.App.Services;

/// <summary>
/// Owns the low-level hooks and re-raises their events on the UI thread,
/// each tagged with whether the mouse was over one of our own windows.
/// The Read button and the quick menu both listen here.
/// </summary>
public sealed class InputMonitor : IDisposable
{
    private readonly InputHooks _hooks;

    public InputMonitor(MessageWindow thread)
    {
        _hooks = new InputHooks(thread);
        _hooks.MouseDown += (button, point, time) => Dispatcher.UIThread.Post(() => MouseDown?.Invoke(button, point, time, IsOurs(point)));
        _hooks.MouseUp += (button, point, time) => Dispatcher.UIThread.Post(() => MouseUp?.Invoke(button, point, time, IsOurs(point)));
        _hooks.Wheel += () => Dispatcher.UIThread.Post(() => Wheel?.Invoke());
        _hooks.KeyPressed += () => Dispatcher.UIThread.Post(() => KeyPressed?.Invoke());
    }

    /// <summary>Button, screen point, timestamp, and whether the point is over one of this app's windows.</summary>
    public event Action<PointerButton, GesturePoint, long, bool>? MouseDown;

    public event Action<PointerButton, GesturePoint, long, bool>? MouseUp;

    public event Action? Wheel;

    public event Action? KeyPressed;

    public bool IsInstalled => _hooks.IsInstalled;

    public bool Install() => _hooks.Install();

    public void Dispose() => _hooks.Dispose();

    private static bool IsOurs(GesturePoint point) => WindowAtPoint.BelongsToThisProcess(WindowAtPoint.Handle(point.X, point.Y));
}
