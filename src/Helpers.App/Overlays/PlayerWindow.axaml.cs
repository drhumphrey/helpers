using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Helpers.App.ViewModels;

namespace Helpers.App.Overlays;

/// <summary>The floating player: a compact bar that expands into the reading view.</summary>
public partial class PlayerWindow : OverlayWindow
{
    private readonly PlayerViewModel _viewModel;

    public PlayerWindow(PlayerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlayerViewModel.CurrentIndex))
            {
                BringCurrentIntoView();
            }
        };
    }

    /// <summary>Raised when the user drags the window somewhere else, so the place can be remembered.</summary>
    public event Action<PixelPoint>? Moved;

    /// <summary>Raised by the cog button.</summary>
    public event Action? SettingsRequested;

    /// <summary>Puts the window at a remembered place if that place is still on a screen, else bottom-right of the mouse's screen.</summary>
    public void PlaceAt(PixelPoint? remembered)
    {
        if (remembered is { } place && Screens.ScreenFromPoint(place) is not null)
        {
            Position = place;
            KeepOnScreen();
            return;
        }

        if (TargetScreen() is { } screen)
        {
            PlaceBottomRight(screen, 8, 120);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        FitReadingViewToScreen();
        PositionChanged += (_, args) => Moved?.Invoke(args.Point);
    }

    private void OnPlayPause(object? sender, RoutedEventArgs e) => _viewModel.TogglePause();

    private void OnBack(object? sender, RoutedEventArgs e) => _viewModel.SkipBack();

    private void OnForward(object? sender, RoutedEventArgs e) => _viewModel.SkipForward();

    private void OnStop(object? sender, RoutedEventArgs e) => _viewModel.Stop();

    private void OnSettings(object? sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

    private void OnToggleExpand(object? sender, RoutedEventArgs e)
    {
        FitReadingViewToScreen();
        _viewModel.IsExpanded = !_viewModel.IsExpanded;
        if (_viewModel.IsExpanded)
        {
            BringCurrentIntoView();
        }
    }

    private void OnHide(object? sender, RoutedEventArgs e) => Hide();

    private void OnSegmentPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: SegmentItem item })
        {
            _viewModel.JumpTo(item.Index);
            e.Handled = true;
        }
    }

    private void OnCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // Dragging starts anywhere on the card that isn't a control.
        if (e.Source is Visual source && source.GetSelfAndVisualAncestors().Any(v => v is Button or Slider or ComboBox or Thumb))
        {
            return;
        }

        BeginMoveDrag(e);
    }

    /// <summary>The reading view may use just over half the height of the screen the player is on.</summary>
    private void FitReadingViewToScreen()
    {
        if (CurrentScreen() is { } screen)
        {
            var usable = screen.WorkingArea.Height / screen.Scaling;
            Scroller.MaxHeight = Math.Max(240, usable * 0.55 - 120);
        }
    }

    private void BringCurrentIntoView()
    {
        if (!_viewModel.IsExpanded || _viewModel.CurrentIndex < 0)
        {
            return;
        }

        var container = SegmentList.ContainerFromIndex(_viewModel.CurrentIndex);
        container?.BringIntoView();
    }
}
