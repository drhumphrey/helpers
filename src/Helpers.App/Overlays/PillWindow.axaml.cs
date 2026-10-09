using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Helpers.App.Overlays;

/// <summary>
/// The small capsule that appears next to the mouse after a selection: Read,
/// Edit (open the text in Compose), and Paste when the clipboard holds text.
/// Three buttons at most, because it is clicked mid-selection.
/// </summary>
public partial class PillWindow : OverlayWindow
{
    private readonly DispatcherTimer _hideTimer;

    public PillWindow()
    {
        InitializeComponent();
        _hideTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Background, (_, _) => HidePill());
    }

    /// <summary>Raised when Read is clicked.</summary>
    public event Action? ReadRequested;

    /// <summary>Raised when Edit is clicked: capture the selection and open it in Compose.</summary>
    public event Action? EditRequested;

    /// <summary>Raised when Paste is clicked: paste the clipboard over the selection.</summary>
    public event Action? PasteRequested;

    /// <summary>Shows the pill just below and to the right of a screen point, kept on that point's screen.</summary>
    public void ShowAt(PixelPoint point, TimeSpan life, bool canPaste)
    {
        _hideTimer.Stop();
        _hideTimer.Interval = life;
        PasteButton.IsVisible = canPaste;

        var screen = Screens.ScreenFromPoint(point) ?? Screens.Primary;
        if (!IsVisible)
        {
            Show();
        }

        if (screen is not null)
        {
            // Offset so the capsule's visible edge sits a little below the cursor, allowing for the shadow margin.
            var margin = (int)(20 * screen.Scaling * UiScaleFactor());
            var size = PixelSize.FromSize(ClientSize, screen.Scaling);
            var area = screen.WorkingArea;
            var x = Math.Clamp(point.X - margin + 4, area.X, Math.Max(area.X, area.Right - size.Width));
            var y = Math.Clamp(point.Y - margin + 14, area.Y, Math.Max(area.Y, area.Bottom - size.Height));
            Position = new PixelPoint(x, y);
        }

        _hideTimer.Start();
    }

    public void HidePill()
    {
        _hideTimer.Stop();
        if (IsVisible)
        {
            Hide();
        }
    }

    private void OnRead(object? sender, RoutedEventArgs e)
    {
        HidePill();
        ReadRequested?.Invoke();
    }

    private void OnEdit(object? sender, RoutedEventArgs e)
    {
        HidePill();
        EditRequested?.Invoke();
    }

    private void OnPaste(object? sender, RoutedEventArgs e)
    {
        HidePill();
        PasteRequested?.Invoke();
    }

    private double UiScaleFactor() =>
        this.FindControl<LayoutTransformControl>("Scaler")?.LayoutTransform is Avalonia.Media.ScaleTransform s ? s.ScaleX : 1.0;
}
