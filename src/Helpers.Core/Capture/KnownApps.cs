namespace Helpers.Core.Capture;

/// <summary>Facts about other programs that change how this app talks to them.</summary>
public static class KnownApps
{
    private static readonly HashSet<string> Terminals = new(StringComparer.OrdinalIgnoreCase)
    {
        "WindowsTerminal", "conhost", "cmd", "powershell", "pwsh", "Code", "Code - Insiders", "Cursor", "wt",
        "mintty", "alacritty", "wezterm-gui", "OpenConsole", "Terminal",
    };

    private static readonly HashSet<string> ScreenCaptureTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "ScreenClippingHost", "SnippingTool", "ScreenSketch", "SnipSketch",
    };

    private static readonly HashSet<string> SystemShell = new(StringComparer.OrdinalIgnoreCase)
    {
        "LockApp", "LogonUI", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost", "SearchApp", "SearchUI",
        "TextInputHost", "ShellHost", "ApplicationFrameHost", "SystemSettings",
    };

    /// <summary>
    /// Parts of Windows itself: the lock screen, Start, search, the touch
    /// keyboard. Never somewhere to send a draft, even when one is in front.
    /// </summary>
    public static bool IsSystemShell(string? processName) => SystemShell.Contains(Trim(processName));

    /// <summary>
    /// Terminals and editors with built-in terminals. In these Ctrl+C means
    /// "interrupt" and Ctrl+V may be eaten by a shell, so the Insert-key
    /// shortcuts are used instead.
    /// </summary>
    public static bool IsTerminal(string? processName) => Terminals.Contains(Trim(processName));

    /// <summary>
    /// Windows' own screen-capture overlays. Dragging out a snip looks exactly
    /// like selecting text, so the Read button must never appear there,
    /// whatever the user's own exclusion list says.
    /// </summary>
    public static bool IsScreenCaptureTool(string? processName) => ScreenCaptureTools.Contains(Trim(processName));

    private static string Trim(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return string.Empty;
        }

        return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName[..^4] : processName;
    }
}
