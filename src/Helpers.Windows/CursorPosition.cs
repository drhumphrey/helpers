using System.Runtime.InteropServices;

namespace Helpers.Windows;

/// <summary>Where the mouse is, in screen pixels. Overlays open on the monitor that holds it.</summary>
public static class CursorPosition
{
    public static (int X, int Y) Get()
    {
        return GetCursorPos(out var point) ? (point.X, point.Y) : (0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);
}
