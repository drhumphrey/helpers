using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Helpers.App.Services;

namespace Helpers.App.Overlays;

/// <summary>One always-present overlay that stacks the toasts above the tray.</summary>
public partial class ToastWindow : OverlayWindow
{
    private readonly ToastService _toasts;

    public ToastWindow(ToastService toasts)
    {
        _toasts = toasts;
        DataContext = toasts;
        InitializeComponent();

        toasts.Changed += () => Dispatcher.UIThread.Post(Reposition);
        SizeChanged += (_, _) => Reposition();
    }

    private void Reposition()
    {
        if (_toasts.Items.Count == 0)
        {
            if (IsVisible)
            {
                Hide();
            }

            return;
        }

        var screen = IsVisible ? HomeScreen() : TargetScreen();
        if (!IsVisible)
        {
            Show();
        }

        if (screen is not null)
        {
            PlaceBottomRight(screen, 0, 0);
        }
    }

    private void OnToastPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: ToastItem item } && item.Kind != ToastKind.Progress)
        {
            _toasts.Dismiss(item);
        }
    }

    private void OnActionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ToastItem item })
        {
            item.Action?.Invoke();
            _toasts.Dismiss(item);
            e.Handled = true;
        }
    }
}
