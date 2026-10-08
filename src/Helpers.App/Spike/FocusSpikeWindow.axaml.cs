using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Helpers.App.Overlays;
using Helpers.Windows;

namespace Helpers.App.Spike;

/// <summary>
/// Milestone 3's first task: prove that an Avalonia overlay can be clicked
/// without stealing focus from the app underneath. The window shows which
/// window has focus, refreshed four times a second.
/// </summary>
public partial class FocusSpikeWindow : OverlayWindow
{
    private readonly DispatcherTimer _timer;
    private int _clicks;

    public FocusSpikeWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Refresh());
        _timer.Start();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Bottom-right of the primary monitor, above the taskbar.
        if (Screens.Primary is { } screen)
        {
            var area = screen.WorkingArea;
            var size = PixelSize.FromSize(ClientSize, screen.Scaling);
            Position = new PixelPoint(area.Right - size.Width - 12, area.Bottom - size.Height - 12);
        }

        StyleText.Text = TryGetPlatformHandle() is { } handle && NonActivatingWindows.IsApplied(handle.Handle)
            ? "No-activate style: applied"
            : "No-activate style: NOT applied";
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }

    private void Refresh()
    {
        var hwnd = ForegroundWindow.Handle;
        var ours = TryGetPlatformHandle() is { } handle && handle.Handle == hwnd;
        FocusText.Text = ours
            ? "Focus is in: THIS OVERLAY (that's a failure)"
            : $"Focus is in: {ForegroundWindow.Describe(hwnd)}";
    }

    private void OnClickMe(object? sender, RoutedEventArgs e)
    {
        _clicks++;
        ClickCount.Text = _clicks == 1 ? "1 click" : $"{_clicks} clicks";
    }
}
