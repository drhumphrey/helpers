using Avalonia.Threading;
using Helpers.Core.Input;
using Helpers.Windows;

namespace Helpers.App.Services;

/// <summary>Owns the message window and the one global shortcut. Re-registers when the setting changes.</summary>
public sealed class HotkeyService : IDisposable
{
    private readonly MessageWindow _window = new();
    private int _hotkeyId = -1;

    public HotkeyService()
    {
        _window.Start();
        _window.HotkeyPressed += id =>
        {
            if (id == _hotkeyId)
            {
                Dispatcher.UIThread.Post(() => Pressed?.Invoke());
            }
        };
    }

    /// <summary>Raised on the UI thread.</summary>
    public event Action? Pressed;

    /// <summary>The shortcut currently registered, or null.</summary>
    public HotkeyGesture? Current { get; private set; }

    /// <summary>Registers the shortcut. Returns false if Windows refused it, usually because another app owns it.</summary>
    public bool Apply(HotkeyGesture? gesture, bool enabled)
    {
        Unregister();
        if (!enabled || gesture is null || !gesture.HasModifier)
        {
            return true;
        }

        var key = VirtualKeys.From(gesture.Key);
        if (key == 0)
        {
            return false;
        }

        _hotkeyId = _window.RegisterHotkey(gesture.Ctrl, gesture.Alt, gesture.Shift, gesture.Win, key);
        if (_hotkeyId > 0)
        {
            Current = gesture;
            return true;
        }

        return false;
    }

    /// <summary>The message window's thread, for anything else that needs a message loop.</summary>
    public MessageWindow Window => _window;

    public void Dispose()
    {
        Unregister();
        _window.Dispose();
    }

    private void Unregister()
    {
        if (_hotkeyId > 0)
        {
            _window.UnregisterHotkey(_hotkeyId);
            _hotkeyId = -1;
        }

        Current = null;
    }
}
