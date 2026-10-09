namespace Helpers.Core.Capture;

/// <summary>Facts about other programs that change how this app talks to them.</summary>
public static class KnownApps
{
    private static readonly HashSet<string> Terminals = new(StringComparer.OrdinalIgnoreCase)
    {
        "WindowsTerminal", "conhost", "cmd", "powershell", "pwsh", "Code", "Code - Insiders", "Cursor", "wt",
        "mintty", "alacritty", "wezterm-gui", "OpenConsole", "Terminal",
    };

    /// <summary>
    /// Terminals and editors with built-in terminals. In these Ctrl+C means
    /// "interrupt" and Ctrl+V may be eaten by a shell, so the Insert-key
    /// shortcuts are used instead.
    /// </summary>
    public static bool IsTerminal(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName[..^4] : processName;
        return Terminals.Contains(name);
    }
}
