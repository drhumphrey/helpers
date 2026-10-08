using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Win32;
using Helpers.Windows;

namespace Helpers.App.Overlays;

/// <summary>
/// The base for every surface that must never take focus: the pill, the
/// player, toasts and the AI result card. Borderless, topmost, translucent
/// where the OS allows it, kept off the taskbar, and told by Windows not to
/// activate when clicked.
/// </summary>
public class OverlayWindow : Window
{
    public OverlayWindow()
    {
        WindowDecorations = WindowDecorations.None;
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = true;
        CanResize = false;
        Background = Brushes.Transparent;
        TransparencyLevelHint =
        [
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Transparent,
            WindowTransparencyLevel.None,
        ];

        if (OperatingSystem.IsWindows())
        {
            Win32Properties.AddWndProcHookCallback(this, RefuseActivation);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
        {
            NonActivatingWindows.Apply(handle.Handle);
        }
    }

    private static nint RefuseActivation(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == NonActivatingWindows.MouseActivateMessage)
        {
            handled = true;
            return NonActivatingWindows.DoNotActivate;
        }

        return 0;
    }
}
