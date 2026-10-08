using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Helpers.App.Services;
using Helpers.App.ViewModels;

namespace Helpers.App.Overlays;

/// <summary>The floating player: a compact bar that expands into a resizable reading view.</summary>
public partial class PlayerWindow : OverlayWindow
{
    private const double DefaultExpandedWidth = 900;
    private const double MinExpandedWidth = 640;
    private const double MinExpandedHeight = 260;

    private readonly PlayerViewModel _viewModel;
    private readonly DispatcherTimer _motion;
    private LinearGradientBrush? _borderMotion;
    private LinearGradientBrush? _highlightMotion;
    private double _phase;
    private bool _resizing;
    private Point _resizeStart;
    private Size _resizeFrom;
    private int _renderedListIndex = -1;

    public PlayerWindow(PlayerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        viewModel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(PlayerViewModel.CurrentIndex):
                    BringCurrentIntoView();
                    RenderListWord();
                    break;
                case nameof(PlayerViewModel.CurrentText):
                case nameof(PlayerViewModel.CurrentWordIndex):
                    RenderSentence();
                    RenderListWord();
                    break;
            }
        };

        // Own copies of the gradient brushes, so they can drift while reading.
        this.GetResourceObservable("OverlayBorderBrush").Subscribe(new AnonymousObserver<object?>(value =>
        {
            _borderMotion = CloneGradient(value);
            Card.BorderBrush = _borderMotion ?? value as IBrush;
        }));
        this.GetResourceObservable("HighlightBrush").Subscribe(new AnonymousObserver<object?>(value =>
        {
            _highlightMotion = CloneGradient(value);
            Highlight.Background = _highlightMotion ?? value as IBrush;
        }));

        _motion = new DispatcherTimer(TimeSpan.FromMilliseconds(40), DispatcherPriority.Render, (_, _) => Drift());
        _motion.Start();
        Closed += (_, _) => _motion.Stop();
    }

    /// <summary>Raised when the user drags the window somewhere else, so the place can be remembered.</summary>
    public event Action<PixelPoint>? Moved;

    /// <summary>Raised by the cog button.</summary>
    public event Action? SettingsRequested;

    /// <summary>Raised after the reading view has been resized by hand, in device-independent pixels.</summary>
    public event Action<double, double>? ReadingViewResized;

    /// <summary>Raised when the reading view is opened or closed.</summary>
    public event Action<bool>? ExpandedChanged;

    public bool AnimateWhileReading { get; set; } = true;

    /// <summary>The size to use for the reading view, or null for the default.</summary>
    public Size? ExpandedSize { get; set; }

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

    /// <summary>Opens or closes the reading view. The sentence box moves to its own line when open.</summary>
    public void SetExpanded(bool expand)
    {
        if (expand == _viewModel.IsExpanded)
        {
            return;
        }

        if (expand)
        {
            var size = ExpandedSize ?? DefaultExpandedSize();
            SizeToContent = SizeToContent.Manual;
            Width = Math.Max(MinExpandedWidth, size.Width);
            Height = Math.Max(MinExpandedHeight, size.Height);

            Grid.SetRow(Highlight, 1);
            Grid.SetColumn(Highlight, 0);
            Grid.SetColumnSpan(Highlight, 11);
            Highlight.Width = double.NaN;
            Highlight.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            Highlight.Margin = new Thickness(0, 10, 0, 0);

            _viewModel.IsExpanded = true;
            BringCurrentIntoView();
            RenderListWord();
        }
        else
        {
            _viewModel.IsExpanded = false;

            Grid.SetRow(Highlight, 0);
            Grid.SetColumn(Highlight, 4);
            Grid.SetColumnSpan(Highlight, 1);
            Highlight.Width = 480;
            Highlight.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            Highlight.Margin = new Thickness(10, 0);

            Width = double.NaN;
            Height = double.NaN;
            SizeToContent = SizeToContent.WidthAndHeight;
        }

        ExpandedChanged?.Invoke(expand);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PositionChanged += (_, args) => Moved?.Invoke(args.Point);
    }

    private void OnPlayPause(object? sender, RoutedEventArgs e) => _viewModel.TogglePause();

    private void OnBack(object? sender, RoutedEventArgs e) => _viewModel.SkipBack();

    private void OnForward(object? sender, RoutedEventArgs e) => _viewModel.SkipForward();

    private void OnStop(object? sender, RoutedEventArgs e) => _viewModel.Stop();

    private void OnSettings(object? sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

    private void OnToggleExpand(object? sender, RoutedEventArgs e) => SetExpanded(!_viewModel.IsExpanded);

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
        if (e.Source is Visual source && source.GetSelfAndVisualAncestors().Any(v => v is Button or Slider or ComboBox or Thumb || v == Grip))
        {
            return;
        }

        BeginMoveDrag(e);
    }

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        _resizing = true;
        _resizeStart = e.GetPosition(this);
        _resizeFrom = new Size(Width, Height);
        e.Pointer.Capture(Grip);
        e.Handled = true;
    }

    private void OnGripMoved(object? sender, PointerEventArgs e)
    {
        if (!_resizing)
        {
            return;
        }

        var now = e.GetPosition(this);
        Width = Math.Max(MinExpandedWidth, _resizeFrom.Width + (now.X - _resizeStart.X));
        Height = Math.Max(MinExpandedHeight, _resizeFrom.Height + (now.Y - _resizeStart.Y));
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
        ExpandedSize = new Size(Width, Height);
        ReadingViewResized?.Invoke(Width, Height);
        e.Handled = true;
    }

    /// <summary>About half the height of the screen the player is on, at the designed width.</summary>
    private Size DefaultExpandedSize()
    {
        var height = 520.0;
        if (HomeScreen() is { } screen)
        {
            height = Math.Max(MinExpandedHeight, screen.WorkingArea.Height / screen.Scaling * 0.55);
        }

        return new Size(DefaultExpandedWidth, height);
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

    /// <summary>
    /// Draws the current sentence word by word in the bar, marks the word being
    /// spoken, and scrolls so that word's line is in view. Nothing is ever cut off.
    /// </summary>
    private void RenderSentence()
    {
        Sentence.Inlines = BuildInlines(_viewModel.Words, _viewModel.CurrentWordIndex, _viewModel.CurrentText, out var currentStart);

        if (currentStart < 0)
        {
            SentenceScroller.Offset = new Vector(0, 0);
            return;
        }

        // Scroll so the current word's line is the top line of the two shown.
        Dispatcher.UIThread.Post(() =>
        {
            var layout = Sentence.TextLayout;
            if (layout is null)
            {
                return;
            }

            var rect = layout.HitTestTextPosition(currentStart);
            var lineTop = Math.Max(0, rect.Top);
            var visible = SentenceScroller.Viewport.Height;
            var offset = SentenceScroller.Offset.Y;
            if (lineTop < offset || rect.Bottom > offset + visible)
            {
                SentenceScroller.Offset = new Vector(0, lineTop);
            }
        }, DispatcherPriority.Loaded);
    }

    /// <summary>Marks the spoken word on the current line of the reading view as well, and clears the line it left.</summary>
    private void RenderListWord()
    {
        if (!_viewModel.IsExpanded)
        {
            return;
        }

        var index = _viewModel.CurrentIndex;
        if (_renderedListIndex >= 0 && _renderedListIndex != index)
        {
            ResetListLine(_renderedListIndex);
            _renderedListIndex = -1;
        }

        if (index < 0 || FindListTextBlock(index) is not { } block)
        {
            return;
        }

        block.Inlines = BuildInlines(_viewModel.Words, _viewModel.CurrentWordIndex, _viewModel.CurrentText, out _);
        _renderedListIndex = index;
    }

    private void ResetListLine(int index)
    {
        if (FindListTextBlock(index) is { } block && index < _viewModel.Segments.Count)
        {
            block.Inlines = new InlineCollection { new Run(_viewModel.Segments[index].Display) };
        }
    }

    private TextBlock? FindListTextBlock(int index) =>
        SegmentList.ContainerFromIndex(index)?.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();

    /// <summary>Words as runs, the current one marked. <paramref name="currentStart"/> is its character index, or -1.</summary>
    private InlineCollection BuildInlines(IReadOnlyList<string> words, int current, string fallback, out int currentStart)
    {
        var inlines = new InlineCollection();
        currentStart = -1;
        if (words.Count == 0)
        {
            inlines.Add(new Run(fallback));
            return inlines;
        }

        var position = 0;
        var wordBrush = this.FindResource("WordBrush") as IBrush;
        for (var i = 0; i < words.Count; i++)
        {
            var run = new Run(words[i]);
            if (i == current)
            {
                run.Background = wordBrush;
                run.FontWeight = FontWeight.SemiBold;
                currentStart = position;
            }

            inlines.Add(run);
            position += words[i].Length;
            if (i < words.Count - 1)
            {
                inlines.Add(new Run(" "));
                position += 1;
            }
        }

        return inlines;
    }

    /// <summary>Moves the gradients a little each tick while reading. Still when paused, stopped or switched off.</summary>
    private void Drift()
    {
        if (!AnimateWhileReading || !_viewModel.ShowPause || !IsVisible)
        {
            return;
        }

        _phase += 0.015;
        if (_borderMotion is not null)
        {
            var x = 0.5 + 0.5 * Math.Cos(_phase);
            var y = 0.5 + 0.5 * Math.Sin(_phase);
            _borderMotion.StartPoint = new RelativePoint(x, y, RelativeUnit.Relative);
            _borderMotion.EndPoint = new RelativePoint(1 - x, 1 - y, RelativeUnit.Relative);
        }

        if (_highlightMotion is not null)
        {
            var x = 0.5 + 0.5 * Math.Cos(_phase * 0.6);
            _highlightMotion.StartPoint = new RelativePoint(x, 0, RelativeUnit.Relative);
            _highlightMotion.EndPoint = new RelativePoint(1 - x, 1, RelativeUnit.Relative);
        }
    }

    private static LinearGradientBrush? CloneGradient(object? value)
    {
        if (value is not LinearGradientBrush source)
        {
            return null;
        }

        var clone = new LinearGradientBrush
        {
            StartPoint = source.StartPoint,
            EndPoint = source.EndPoint,
        };

        foreach (var stop in source.GradientStops)
        {
            clone.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));
        }

        return clone;
    }
}
