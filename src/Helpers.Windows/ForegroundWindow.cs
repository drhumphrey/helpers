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

    /// <summary>The centre of the window in screen pixels, or null if it has no rectangle.</summary>
    public static (int X, int Y)? Centre(nint hwnd)
    {
        if (hwnd == 0 || !NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return null;
        }

        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
        {
            return null;
        }

        return ((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
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

    /// <summary>The window class, such as "Shell_TrayWnd" for the taskbar.</summary>
    public static string ClassName(nint hwnd)
    {
        if (hwnd == 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    /// <summary>
    /// The product name from the program's own file details, such as "Visual
    /// Studio Code" for Code.exe. Falls back to the process name when Windows
    /// won't say, as it won't for some packaged apps.
    /// </summary>
    public static string FriendlyName(nint hwnd)
    {
        if (hwnd == 0)
        {
            return string.Empty;
        }

        var processName = ProcessName(hwnd);
        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var description = process.MainModule?.FileVersionInfo.FileDescription?.Trim();
            return string.IsNullOrEmpty(description) ? processName : description;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return processName;
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
