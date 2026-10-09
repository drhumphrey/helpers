using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using Helpers.Core.Spelling;

namespace Helpers.App.Controls;

/// <summary>
/// Draws a wavy red line under each misspelt word in a TextBox. It sits over
/// the box in the same grid cell, takes no input, and asks the box's own text
/// layout where each word is, so wrapping and scrolling come for free.
/// Red in every vibe, as the design notes say.
/// </summary>
public sealed class SpellingUnderlines : Control
{
    private static readonly IPen WavePen = new Pen(new SolidColorBrush(Color.Parse("#FF5A5A")), 1.4);
    private const double Step = 3;
    private const double Amplitude = 1.4;

    private TextBox? _editor;
    private TextPresenter? _presenter;
    private ScrollViewer? _scroller;
    private IReadOnlyList<SpellingError> _errors = [];

    public SpellingUnderlines()
    {
        IsHitTestVisible = false;
    }

    /// <summary>The text box to decorate. Set once, after the box is in the tree.</summary>
    public TextBox? Editor
    {
        get => _editor;
        set
        {
            if (_editor is not null)
            {
                _editor.TemplateApplied -= OnTemplateApplied;
                _editor.SizeChanged -= OnChanged;
                DetachParts();
            }

            _editor = value;
            if (_editor is not null)
            {
                _editor.TemplateApplied += OnTemplateApplied;
                _editor.SizeChanged += OnChanged;
                FindParts();
            }

            InvalidateVisual();
        }
    }

    public IReadOnlyList<SpellingError> Errors
    {
        get => _errors;
        set
        {
            _errors = value;
            InvalidateVisual();
        }
    }

    /// <summary>The character index under a point in this control's coordinates, or -1.</summary>
    public int IndexAt(Point point)
    {
        if (_presenter?.TextLayout is null)
        {
            return -1;
        }

        var local = this.TranslatePoint(point, _presenter);
        if (local is null)
        {
            return -1;
        }

        return _presenter.TextLayout.HitTestPoint(local.Value).TextPosition;
    }

    public override void Render(DrawingContext context)
    {
        if (_editor is null || _presenter?.TextLayout is null || _scroller is null || _errors.Count == 0)
        {
            return;
        }

        var text = _editor.Text ?? string.Empty;
        var origin = _presenter.TranslatePoint(new Point(0, 0), this);
        var viewport = _scroller.TranslatePoint(new Point(0, 0), this);
        if (origin is null || viewport is null)
        {
            return;
        }

        var clip = new Rect(viewport.Value, _scroller.Bounds.Size);
        var shift = new Vector(origin.Value.X, origin.Value.Y);
        using (context.PushClip(clip))
        {
            foreach (var error in _errors)
            {
                if (error.Start < 0 || error.End > text.Length)
                {
                    continue;
                }

                foreach (var rect in _presenter.TextLayout.HitTestTextRange(error.Start, error.Length))
                {
                    var line = rect.Translate(shift);
                    if (line.Width < 1 || !clip.Intersects(line))
                    {
                        continue;
                    }

                    DrawWave(context, line.Left, line.Right, line.Bottom - 1.5);
                }
            }
        }
    }

    private static void DrawWave(DrawingContext context, double left, double right, double y)
    {
        var geometry = new StreamGeometry();
        using (var figure = geometry.Open())
        {
            figure.BeginFigure(new Point(left, y), isFilled: false);
            var up = true;
            for (var x = left + Step; x < right + Step; x += Step)
            {
                figure.LineTo(new Point(Math.Min(x, right), up ? y - Amplitude : y + Amplitude));
                up = !up;
            }

            figure.EndFigure(isClosed: false);
        }

        context.DrawGeometry(null, WavePen, geometry);
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        DetachParts();
        FindParts();
    }

    private void FindParts()
    {
        if (_editor is null)
        {
            return;
        }

        _presenter = _editor.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        _scroller = _editor.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (_scroller is not null)
        {
            _scroller.ScrollChanged += OnScrollChanged;
        }

        if (_presenter is not null)
        {
            _presenter.LayoutUpdated += OnChanged;
        }
    }

    private void DetachParts()
    {
        if (_scroller is not null)
        {
            _scroller.ScrollChanged -= OnScrollChanged;
            _scroller = null;
        }

        if (_presenter is not null)
        {
            _presenter.LayoutUpdated -= OnChanged;
            _presenter = null;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => InvalidateVisual();

    private void OnChanged(object? sender, EventArgs e) => InvalidateVisual();
}
