using System.Runtime.InteropServices;
using System.Text;

namespace Helpers.Windows.Interop;

/// <summary>Win32 entry points used by this project. Keep each one documented with why it is needed.</summary>
internal static class NativeMethods
{
    public const int GWL_EXSTYLE = -20;

    public const long WS_EX_TOPMOST = 0x00000008;
    public const long WS_EX_TOOLWINDOW = 0x00000080;
    public const long WS_EX_NOACTIVATE = 0x08000000;

    public const uint WM_MOUSEACTIVATE = 0x0021;
    public const int MA_NOACTIVATE = 3;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    public static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowText(nint hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(nint hWnd, out RECT rect);
}
