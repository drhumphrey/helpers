using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Win32;
using Helpers.Windows;

namespace Helpers.App.Overlays;

/// <summary>
/// The base for every surface that must never take focus: the pill, the
/// player, toasts and the AI result card. Borderless, topmost, transparent
/// outside its card, kept off the taskbar, told by Windows not to activate
/// when clicked, and always kept inside the screen it is on.
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

        // Plain transparency, not acrylic: Windows applies acrylic blur to the whole
        // window rectangle, which turns the shadow margin into a frosted box.
        TransparencyLevelHint =
        [
            WindowTransparencyLevel.Transparent,
            WindowTransparencyLevel.None,
        ];

        if (OperatingSystem.IsWindows())
        {
            Win32Properties.AddWndProcHookCallback(this, RefuseActivation);
        }

        SizeChanged += (_, _) => KeepOnScreen();
    }

    /// <summary>The screen the mouse is on, falling back to the primary one.</summary>
    public Screen? TargetScreen()
    {
        if (OperatingSystem.IsWindows())
        {
            var (x, y) = CursorPosition.Get();
            var screen = Screens.ScreenFromPoint(new PixelPoint(x, y));
            if (screen is not null)
            {
                return screen;
            }
        }

        return Screens.Primary;
    }

    /// <summary>The screen this window is on, falling back to the primary one.</summary>
    public Screen? CurrentScreen() => Screens.ScreenFromWindow(this) ?? Screens.Primary;

    /// <summary>The window's size in screen pixels on a given screen.</summary>
    public PixelSize PixelSizeOn(Screen screen) => PixelSize.FromSize(ClientSize, screen.Scaling);

    /// <summary>Places the window at the bottom-right of a screen's working area, with a margin.</summary>
    public void PlaceBottomRight(Screen screen, int marginRight, int marginBottom)
    {
        var area = screen.WorkingArea;
        var size = PixelSizeOn(screen);
        Position = new PixelPoint(area.Right - size.Width - marginRight, area.Bottom - size.Height - marginBottom);
    }

    /// <summary>Nudges the window back inside its screen's working area if any edge has drifted off.</summary>
    public void KeepOnScreen()
    {
        if (!IsVisible || CurrentScreen() is not { } screen)
        {
            return;
        }

        var area = screen.WorkingArea;
        var size = PixelSizeOn(screen);
        var x = Math.Clamp(Position.X, area.X, Math.Max(area.X, area.Right - size.Width));
        var y = Math.Clamp(Position.Y, area.Y, Math.Max(area.Y, area.Bottom - size.Height));
        if (x != Position.X || y != Position.Y)
        {
            Position = new PixelPoint(x, y);
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
