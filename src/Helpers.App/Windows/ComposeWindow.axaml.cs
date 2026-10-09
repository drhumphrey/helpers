using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Helpers.Core.Capture;
using Helpers.Core.Spelling;

namespace Helpers.App.Windows;

/// <summary>
/// A plain window for writing to the AI: a big text box with spelling as you
/// type, Read back, Copy and Send to chat. It hides rather than closes so the
/// draft and the checker stay warm. The AI buttons arrive in a later milestone.
/// </summary>
public partial class ComposeWindow : Window
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan DraftDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PlacementDelay = TimeSpan.FromMilliseconds(800);

    private readonly DraftSpelling _spelling;
    private readonly MenuFlyout _menu = new();
    private readonly DispatcherTimer _checkTimer;
    private readonly DispatcherTimer _draftTimer;
    private readonly DispatcherTimer _placementTimer;
    private IReadOnlyList<SpellingError> _errors = [];
    private string _errorsText = string.Empty;
    private int _menuIndex = -1;
    private bool _draftDirty;
    private bool _loading;
    private bool _opened;

    public ComposeWindow(DraftSpelling spelling)
    {
        _spelling = spelling;
        InitializeComponent();

        Underlines.Editor = Editor;
        Editor.ContextFlyout = _menu;
        _menu.Opening += OnMenuOpening;
        Editor.AddHandler(PointerPressedEvent, OnEditorPointerPressed, RoutingStrategies.Tunnel);
        Editor.GotFocus += (_, _) => EditorFrame.Classes.Add("focused");
        Editor.LostFocus += (_, _) => EditorFrame.Classes.Remove("focused");
        Editor.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.CaretIndexProperty)
            {
                RestartCheck();
            }
        };

        _checkTimer = new DispatcherTimer(CheckDelay, DispatcherPriority.Background, (_, _) => CheckNow());
        _draftTimer = new DispatcherTimer(DraftDelay, DispatcherPriority.Background, (_, _) => SaveDraft());
        _placementTimer = new DispatcherTimer(PlacementDelay, DispatcherPriority.Background, (timer, _) =>
        {
            (timer as DispatcherTimer)?.Stop();
            if (_opened && WindowState == WindowState.Normal)
            {
                PlacementChanged?.Invoke(Position, ClientSize);
            }
        });
        _checkTimer.Stop();
        _draftTimer.Stop();
        _placementTimer.Stop();

        PositionChanged += (_, _) => RestartPlacementTimer();
        SizeChanged += (_, _) => RestartPlacementTimer();
        KeyDown += OnKeyDown;

        ShowSpellingState();
    }

    public event Action<string>? ReadBackRequested;

    public event Action<string>? CopyRequested;

    public event Action<string>? SendRequested;

    public event Action? UseThisWindowRequested;

    /// <summary>Raised a couple of seconds after the last edit, and on hide, with the whole draft.</summary>
    public event Action<string>? DraftChanged;

    /// <summary>Raised when the window has settled somewhere new, in screen pixels and client size.</summary>
    public event Action<PixelPoint, Size>? PlacementChanged;

    /// <summary>The whole draft. Setting it is a load, not an edit: nothing is saved and a fresh check starts.</summary>
    public string Text
    {
        get => Editor.Text ?? string.Empty;
        set
        {
            _loading = true;
            try
            {
                Editor.Text = value;
                Editor.CaretIndex = value.Length;
            }
            finally
            {
                _loading = false;
            }

            _errors = [];
            _errorsText = value;
            Underlines.Errors = _errors;
            _draftDirty = false;
            _draftTimer.Stop();
            DraftStatus.Text = string.Empty;
            RestartCheck();
        }
    }

    /// <summary>Shows which window Send will paste into.</summary>
    public void SetTarget(ITargetWindow? target)
    {
        if (target is null)
        {
            TargetLabel.Text = "Sending to: nothing yet";
            ToolTip.SetTip(TargetLabel, "Press Use this window, then click the window you want.");
        }
        else
        {
            TargetLabel.Text = $"Sending to: {target.AppName}";
            ToolTip.SetTip(TargetLabel, target.Title.Length > 0 ? target.Title : target.AppName);
        }
    }

    /// <summary>While the user is picking a window, says so in place of the target.</summary>
    public void ShowPicking(bool picking)
    {
        UseWindowButton.IsEnabled = !picking;
        if (picking)
        {
            TargetLabel.Text = "Click the window you want to send to…";
        }
    }

    public void ShowDraftSaved()
    {
        DraftStatus.Text = "Draft saved";
    }

    public void FocusEditor()
    {
        Editor.Focus();
        Editor.CaretIndex = Text.Length;
    }

    /// <summary>Hides instead of closing, unless the app itself is shutting down.</summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic)
        {
            e.Cancel = true;
            HideKeepingDraft();
        }

        base.OnClosing(e);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _opened = true;
        FocusEditor();
        CheckNow();
    }

    public void HideKeepingDraft()
    {
        if (_draftDirty)
        {
            SaveDraft();
        }

        Hide();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SendRequested?.Invoke(Text);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && !_menu.IsOpen)
        {
            HideKeepingDraft();
            e.Handled = true;
        }
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // The box raises this after the fact, so a load echoes here once the loading flag is down;
        // the content is already known then, and nothing changed.
        var text = Text;
        if (_loading || string.Equals(text, _errorsText, StringComparison.Ordinal))
        {
            return;
        }

        // Keep the underlines in place while waiting for the next check.
        _errors = SpellingErrors.Shift(_errors, _errorsText, text);
        _errorsText = text;
        Underlines.Errors = _errors;

        RestartCheck();

        _draftDirty = true;
        DraftStatus.Text = string.Empty;
        _draftTimer.Stop();
        _draftTimer.Start();
    }

    private void RestartCheck()
    {
        if (!_spelling.IsAvailable)
        {
            return;
        }

        _checkTimer.Stop();
        _checkTimer.Start();
    }

    private void CheckNow()
    {
        _checkTimer.Stop();
        if (!_spelling.IsAvailable)
        {
            return;
        }

        var text = Text;
        _errors = _spelling.Check(text, Editor.CaretIndex);
        _errorsText = text;
        Underlines.Errors = _errors;
    }

    private void SaveDraft()
    {
        _draftTimer.Stop();
        if (!_draftDirty)
        {
            return;
        }

        _draftDirty = false;
        DraftChanged?.Invoke(Text);
    }

    private void RestartPlacementTimer()
    {
        if (!_opened)
        {
            return;
        }

        _placementTimer.Stop();
        _placementTimer.Start();
    }

    private void ShowSpellingState()
    {
        if (_spelling.IsAvailable)
        {
            SpellingStatus.Text = "Spelling: English (UK). Right-click a marked word for suggestions.";
            LanguageButton.IsVisible = false;
        }
        else
        {
            SpellingStatus.Text = "Spelling is off: Windows has no English (United Kingdom) checker installed.";
            LanguageButton.IsVisible = true;
        }
    }

    // The right-click menu: suggestions for the word under the pointer, then the usual editing items.

    private void OnEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(Editor);
        _menuIndex = point.Properties.IsRightButtonPressed ? Underlines.IndexAt(e.GetPosition(Underlines)) : -1;
    }

    private void OnMenuOpening(object? sender, EventArgs e)
    {
        _menu.Items.Clear();

        var text = Text;
        var index = _menuIndex >= 0 ? _menuIndex : Editor.CaretIndex;
        _menuIndex = -1;
        var error = _spelling.IsAvailable ? SpellingErrors.FindAt(_errors, index) : null;

        if (error is not null && error.End <= text.Length)
        {
            var word = DraftSpelling.WordAt(text, error);
            var suggestions = _spelling.Suggest(word);
            if (suggestions.Count == 0)
            {
                _menu.Items.Add(new MenuItem { Header = "No suggestions", IsEnabled = false });
            }

            foreach (var suggestion in suggestions)
            {
                var item = new MenuItem { Header = suggestion, FontWeight = FontWeight.SemiBold };
                var replacement = suggestion;
                var range = error;
                item.Click += (_, _) => Replace(range, replacement);
                _menu.Items.Add(item);
            }

            _menu.Items.Add(new Separator());

            var add = new MenuItem { Header = $"Add \"{word}\" to dictionary" };
            add.Click += (_, _) =>
            {
                _spelling.AddToDictionary(word);
                CheckNow();
            };
            _menu.Items.Add(add);

            var ignore = new MenuItem { Header = "Ignore for now" };
            ignore.Click += (_, _) =>
            {
                _spelling.Ignore(word);
                CheckNow();
            };
            _menu.Items.Add(ignore);
            _menu.Items.Add(new Separator());
        }

        var cut = new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) };
        cut.Click += (_, _) => Editor.Cut();
        var copy = new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        copy.Click += (_, _) => Editor.Copy();
        var paste = new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) };
        paste.Click += (_, _) => Editor.Paste();
        var selectAll = new MenuItem { Header = "Select all", InputGesture = new KeyGesture(Key.A, KeyModifiers.Control) };
        selectAll.Click += (_, _) => Editor.SelectAll();
        _menu.Items.Add(cut);
        _menu.Items.Add(copy);
        _menu.Items.Add(paste);
        _menu.Items.Add(selectAll);
    }

    /// <summary>Swaps one word through the box's own editing path, so Ctrl+Z undoes it.</summary>
    private void Replace(SpellingError range, string replacement)
    {
        if (range.End > Text.Length)
        {
            return;
        }

        Editor.SelectionStart = range.Start;
        Editor.SelectionEnd = range.End;
        Editor.SelectedText = replacement;
        Editor.CaretIndex = range.Start + replacement.Length;
        Editor.Focus();
        CheckNow();
    }

    private void OnReadBack(object? sender, RoutedEventArgs e) => ReadBackRequested?.Invoke(Text);

    private void OnCopy(object? sender, RoutedEventArgs e) => CopyRequested?.Invoke(Text);

    private void OnSend(object? sender, RoutedEventArgs e) => SendRequested?.Invoke(Text);

    private void OnUseThisWindow(object? sender, RoutedEventArgs e) => UseThisWindowRequested?.Invoke();

    private void OnOpenLanguageSettings(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            SpellingStatus.Text = "Open Windows Settings, then Time & language, Language & region, and add English (United Kingdom).";
        }
    }
}
