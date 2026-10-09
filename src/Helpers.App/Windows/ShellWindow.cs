using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Helpers.App.Services;

namespace Helpers.App.Windows;

/// <summary>
/// A normal, focusable window drawn in the vibe: no system chrome, a
/// transparent band for the glow, the same card, border and shadow as the
/// player, a title row that drags, and the user's size setting. The look
/// itself is the "shell" window template in Themes/Controls.axaml.
/// Overlays are not shells; they must never take focus.
/// </summary>
public class ShellWindow : Window
{
    private LayoutTransformControl? _scaler;
    private Border? _grip;
    private bool _resizing;
    private Point _resizeStart;
    private Size _resizeFrom;

    public ShellWindow()
    {
        Classes.Add("shell");
        WindowDecorations = WindowDecorations.None;
        Background = Brushes.Transparent;
        TransparencyLevelHint =
        [
            WindowTransparencyLevel.Transparent,
            WindowTransparencyLevel.None,
        ];

        UiScale.Changed += ApplyScale;
        Closed += (_, _) => UiScale.Changed -= ApplyScale;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scaler = e.NameScope.Find<LayoutTransformControl>("PART_Scaler");
        ApplyScale(UiScale.Current);

        if (e.NameScope.Find<Control>("PART_TitleBar") is { } titleBar)
        {
            titleBar.PointerPressed += OnTitleBarPressed;
        }

        if (e.NameScope.Find<Button>("PART_Minimise") is { } minimise)
        {
            minimise.Click += (_, _) => WindowState = WindowState.Minimized;
        }

        if (e.NameScope.Find<Button>("PART_Close") is { } close)
        {
            close.Click += (_, _) => RequestClose();
        }

        _grip = e.NameScope.Find<Border>("PART_Grip");
        if (_grip is not null)
        {
            _grip.PointerPressed += OnGripPressed;
            _grip.PointerMoved += OnGripMoved;
            _grip.PointerReleased += OnGripReleased;
        }
    }

    /// <summary>The title row's close mark. Windows that hide rather than close override this.</summary>
    protected virtual void RequestClose() => Close();

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is Visual source && source.GetSelfAndVisualAncestors().Any(v => v is Button))
        {
            return;
        }

        BeginMoveDrag(e);
    }

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_grip is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _resizing = true;
        _resizeStart = e.GetPosition(this);
        _resizeFrom = new Size(
            double.IsNaN(Width) ? ClientSize.Width : Width,
            double.IsNaN(Height) ? ClientSize.Height : Height);
        e.Pointer.Capture(_grip);
        e.Handled = true;
    }

    private void OnGripMoved(object? sender, PointerEventArgs e)
    {
        if (!_resizing)
        {
            return;
        }

        var now = e.GetPosition(this);
        var minWidth = MinWidth > 0 ? MinWidth : 360;
        var minHeight = MinHeight > 0 ? MinHeight : 240;
        Width = Math.Max(minWidth, _resizeFrom.Width + (now.X - _resizeStart.X));
        Height = Math.Max(minHeight, _resizeFrom.Height + (now.Y - _resizeStart.Y));
        e.Handled = true;
    }

    private void OnGripReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_resizing)
        {
            return;
        }

        _resizing = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ApplyScale(double scale)
    {
        if (_scaler is not null)
        {
            _scaler.LayoutTransform = new ScaleTransform(scale, scale);
        }
    }
}
