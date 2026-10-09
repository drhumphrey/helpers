using Avalonia.Threading;
using Helpers.Core.Input;
using Helpers.Windows;

namespace Helpers.App.Services;

/// <summary>
/// Owns the message window and the global shortcuts, one per name: "read"
/// for the selection, "compose" for the Compose window. Each re-registers
/// when its setting changes.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private readonly MessageWindow _window = new();
    private readonly Dictionary<string, int> _ids = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Action> _actions = new();
    private readonly object _sync = new();

    public HotkeyService()
    {
        _window.Start();
        _window.HotkeyPressed += id =>
        {
            Action? action;
            lock (_sync)
            {
                _actions.TryGetValue(id, out action);
            }

            if (action is not null)
            {
                Dispatcher.UIThread.Post(action);
            }
        };
    }

    /// <summary>The message window's thread, for anything else that needs a message loop.</summary>
    public MessageWindow Window => _window;

    /// <summary>
    /// Registers, or re-registers, one named shortcut. Off, missing or
    /// modifier-less gestures just unregister. Returns false if Windows
    /// refused the combination, usually because another app owns it.
    /// </summary>
    public bool Apply(string name, HotkeyGesture? gesture, bool enabled, Action onPressed)
    {
        Unregister(name);
        if (!enabled || gesture is null || !gesture.HasModifier)
        {
            return true;
        }

        var key = VirtualKeys.From(gesture.Key);
        if (key == 0)
        {
            return false;
        }

        var id = _window.RegisterHotkey(gesture.Ctrl, gesture.Alt, gesture.Shift, gesture.Win, key);
        if (id <= 0)
        {
            return false;
        }

        lock (_sync)
        {
            _ids[name] = id;
            _actions[id] = onPressed;
        }

        return true;
    }

    public void Dispose()
    {
        foreach (var name in _ids.Keys.ToArray())
        {
            Unregister(name);
        }

        _window.Dispose();
    }

    private void Unregister(string name)
    {
        int id;
        lock (_sync)
        {
            if (!_ids.Remove(name, out id))
            {
                return;
            }

            _actions.Remove(id);
        }

        _window.UnregisterHotkey(id);
    }
}
