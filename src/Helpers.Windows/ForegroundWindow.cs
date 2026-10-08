using System.Diagnostics;
using System.Text;
using Helpers.Windows.Interop;

namespace Helpers.Windows;

/// <summary>Which window has keyboard focus right now, and what it is called.</summary>
public static class ForegroundWindow
{
    public static nint Handle => NativeMethods.GetForegroundWindow();

    public static string Title(nint hwnd)
    {
        if (hwnd == 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    public static uint ProcessId(nint hwnd)
    {
        if (hwnd == 0)
        {
            return 0;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return processId;
    }

    public static string ProcessName(nint hwnd)
    {
        if (hwnd == 0)
        {
            return string.Empty;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
    }

    /// <summary>A one-line description such as "Notepad (notepad)".</summary>
    public static string Describe(nint hwnd)
    {
        var title = Title(hwnd);
        var process = ProcessName(hwnd);
        if (title.Length == 0 && process.Length == 0)
        {
            return "nothing";
        }

        return title.Length == 0 ? process : $"{title} ({process})";
    }
}
