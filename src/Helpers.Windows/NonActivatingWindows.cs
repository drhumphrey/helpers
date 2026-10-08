using Helpers.Windows.Interop;

namespace Helpers.Windows;

/// <summary>
/// Makes a native window one that never takes keyboard focus and never shows
/// in the taskbar. The pill, the player and the toasts all need this, or
/// clicking them would pull focus away from the app being read.
/// </summary>
public static class NonActivatingWindows
{
    /// <summary>The message Windows sends before activating a window on a mouse click.</summary>
    public const uint MouseActivateMessage = NativeMethods.WM_MOUSEACTIVATE;

    /// <summary>The reply to that message that means "handle the click but don't activate me".</summary>
    public const nint DoNotActivate = NativeMethods.MA_NOACTIVATE;

    /// <summary>Adds the no-activate, tool-window and topmost extended styles to an existing window.</summary>
    public static void Apply(nint hwnd)
    {
        if (hwnd == 0)
        {
            throw new ArgumentException("A window handle is required.", nameof(hwnd));
        }

        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        style |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (nint)style);
    }

    /// <summary>True when the window already carries the no-activate style.</summary>
    public static bool IsApplied(nint hwnd)
    {
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        return (style & NativeMethods.WS_EX_NOACTIVATE) != 0;
    }
}
