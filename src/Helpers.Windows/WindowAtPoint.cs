using System.Runtime.InteropServices;
using Helpers.Windows.Interop;

namespace Helpers.Windows;

/// <summary>What sits under a screen point: which window, whose process, and whether it's a title bar, border or scrollbar.</summary>
public static class WindowAtPoint
{
    private const uint WM_NCHITTEST = 0x0084;
    private const int HTCLIENT = 1;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    public static nint Handle(int x, int y) => WindowFromPoint(new POINT { X = x, Y = y });

    /// <summary>The top-level window that owns the window at the point.</summary>
    public static nint TopLevelHandle(int x, int y)
    {
        var hwnd = Handle(x, y);
        return hwnd == 0 ? 0 : GetAncestor(hwnd, 2 /* GA_ROOT */);
    }

    public static bool BelongsToThisProcess(nint hwnd)
    {
        if (hwnd == 0)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return processId == (uint)Environment.ProcessId;
    }

    public static string ProcessName(nint hwnd) => ForegroundWindow.ProcessName(hwnd);

    /// <summary>
    /// True when the point is on the window's frame rather than its content:
    /// title bar, borders, scrollbars, buttons. Asks the window, with a short
    /// timeout so a hung app can't stall us.
    /// </summary>
    public static bool IsOnFrame(nint hwnd, int x, int y)
    {
        if (hwnd == 0)
        {
            return false;
        }

        var lParam = (nint)((y << 16) | (x & 0xFFFF));
        if (SendMessageTimeout(hwnd, WM_NCHITTEST, 0, lParam, SMTO_ABORTIFHUNG, 50, out var result) == 0)
        {
            return false;
        }

        return (int)result != HTCLIENT;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint hwnd, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeout(nint hWnd, uint msg, nint wParam, nint lParam, uint flags, uint timeout, out nint result);
}
