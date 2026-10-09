using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Helpers.App.Services;
using Helpers.Core.Ai;
using Helpers.Core.Capture;
using Helpers.Core.Settings;
using Helpers.Core.Spelling;

namespace Helpers.App.Windows;

/// <summary>
/// A plain window for writing to the AI: a big text box with spelling as you
/// type, Read back, Copy and Send to chat, and the AI helper's notes beside
/// the draft. It hides rather than closes so the draft and the checker stay warm.
/// </summary>
public partial class ComposeWindow : ShellWindow
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan DraftDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PlacementDelay = TimeSpan.FromMilliseconds(800);

    private readonly DraftSpelling _spelling;
    private readonly AssistantService _assistant;
    private readonly SettingsStore _settings;
    private readonly ObservableCollection<WritingNote> _notes = [];
    private MenuFlyout _menu = new();
    private readonly DispatcherTimer _checkTimer;
    private readonly DispatcherTimer _draftTimer;
    private readonly DispatcherTimer _placementTimer;
    private IReadOnlyList<SpellingError> _errors = [];
    private string _errorsText = string.Empty;
    private int _typingCaret = -1;
    private bool _draftDirty;
    private bool _loading;
    private bool _opened;
    private CancellationTokenSource? _aiRun;
    private AssistantAction? _pendingAction;
    private string _resultText = string.Empty;

    public ComposeWindow(DraftSpelling spelling, AssistantService assistant, SettingsStore settings)
    {
        _spelling = spelling;
        _assistant = assistant;
        _settings = settings;
        InitializeComponent();

        NotesList.ItemsSource = _notes;
        _assistant.Changed += RefreshAiState;
        Closed += (_, _) => _assistant.Changed -= RefreshAiState;

        Underlines.Editor = Editor;
        Editor.ContextFlyout = _menu;
        Editor.AddHandler(PointerPressedEvent, OnEditorPointerPressed, RoutingStrategies.Tunnel);
        Editor.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
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
        RefreshAiState();
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
            _typingCaret = -1;
            Underlines.Errors = _errors;
            _draftDirty = false;
            _draftTimer.Stop();
            DraftStatus.Text = string.Empty;
            RestartCheck();
        }
    }

    /// <summary>The title row's close mark hides, like the window's own close, so the draft and checker stay warm.</summary>
    protected override void RequestClose() => HideKeepingDraft();

    /// <summary>True when the text came from Edit on the pill and the main button says "Put it back".</summary>
    public bool IsEditing { get; private set; }

    /// <summary>Switches the main button between sending to a chat and putting edited text back.</summary>
    public void SetEditingMode(bool editing)
    {
        IsEditing = editing;
        SendButton.Content = editing ? "Put it back" : "Send to chat";
        ToolTip.SetTip(SendButton, editing
            ? "Pastes the text over the selection in the window it came from. Ctrl+Enter does the same."
            : "Pastes the draft into the window above. It never presses Enter; you do. Ctrl+Enter does the same.");
    }

    /// <summary>
    /// Puts selected text from another app into the editor through the box's
    /// own editing path, so an unsent draft is one Ctrl+Z away.
    /// </summary>
    public void LoadForEditing(string text)
    {
        if (Text.Length == 0)
        {
            Text = text;
            return;
        }

        Editor.SelectAll();
        Editor.SelectedText = text;
        Editor.CaretIndex = text.Length;
        _typingCaret = -1;
        RestartCheck();
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

        // Keep the underlines and the notes in place while waiting for the next check.
        _errors = SpellingErrors.Shift(_errors, _errorsText, text);
        ShiftNotes(_errorsText, text);
        _errorsText = text;
        _typingCaret = Editor.CaretIndex;
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

        // The word under the caret is left alone only while it is being typed. A caret
        // that got there by a click, including the right-click that opens the menu,
        // must not make an underline vanish.
        var text = Text;
        var typing = Editor.CaretIndex == _typingCaret ? _typingCaret : -1;
        _errors = _spelling.Check(text, typing);
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
    //
    // The menu is built on the press, before Avalonia opens it on the release. A flyout
    // creates its presenter before raising Opening, and items added in Opening never
    // reach that presenter, so the menu opened as an empty sliver when built there.
    // A fresh flyout each time also means a cached presenter can never show stale items.

    private void OnEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(Editor).Properties.IsRightButtonPressed)
        {
            PrepareMenu(Underlines.IndexAt(e.GetPosition(Underlines)));
        }
    }

    /// <summary>The keyboard's way in: the Menu key or Shift+F10 open the menu at the caret.</summary>
    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Apps || (e.Key == Key.F10 && e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
        {
            PrepareMenu(Editor.CaretIndex);
        }
    }

    private void PrepareMenu(int index)
    {
        var menu = new MenuFlyout();
        var text = Text;
        if (index < 0)
        {
            index = Editor.CaretIndex;
        }

        var error = _spelling.IsAvailable ? SpellingErrors.FindAt(_errors, index) ?? MisspeltWordAt(text, index) : null;

        if (error is not null && error.End <= text.Length)
        {
            var word = DraftSpelling.WordAt(text, error);
            var suggestions = _spelling.Suggest(word);
            if (suggestions.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = "No suggestions", IsEnabled = false });
            }

            foreach (var suggestion in suggestions)
            {
                var item = new MenuItem { Header = suggestion, FontWeight = FontWeight.SemiBold };
                var replacement = suggestion;
                var range = error;
                item.Click += (_, _) => Replace(range, replacement);
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            var add = new MenuItem { Header = $"Add \"{word}\" to dictionary" };
            add.Click += (_, _) =>
            {
                _spelling.AddToDictionary(word);
                CheckNow();
            };
            menu.Items.Add(add);

            var ignore = new MenuItem { Header = "Ignore for now" };
            ignore.Click += (_, _) =>
            {
                _spelling.Ignore(word);
                CheckNow();
            };
            menu.Items.Add(ignore);
            menu.Items.Add(new Separator());
        }

        var cut = new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) };
        cut.Click += (_, _) => Editor.Cut();
        var copy = new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        copy.Click += (_, _) => Editor.Copy();
        var paste = new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) };
        paste.Click += (_, _) => Editor.Paste();
        var selectAll = new MenuItem { Header = "Select all", InputGesture = new KeyGesture(Key.A, KeyModifiers.Control) };
        selectAll.Click += (_, _) => Editor.SelectAll();
        menu.Items.Add(cut);
        menu.Items.Add(copy);
        menu.Items.Add(paste);
        menu.Items.Add(selectAll);

        _menu = menu;
        Editor.ContextFlyout = menu;
    }

    /// <summary>
    /// A second opinion for the menu: ask the checker about the word under the
    /// pointer directly, so the menu is right even if the underlines are a
    /// pause behind the text.
    /// </summary>
    private SpellingError? MisspeltWordAt(string text, int index)
    {
        var word = SpellingErrors.WordAt(text, index);
        if (word is null)
        {
            return null;
        }

        return _spelling.Check(DraftSpelling.WordAt(text, word), -1).Count > 0 ? word : null;
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

    // The AI helper: notes pinned to the draft, or new text beside it. Nothing replaces the draft by itself.

    private void RefreshAiState()
    {
        var why = _assistant.WhyNotReady();
        var ready = why is null;
        CheckButton.IsEnabled = ready;
        TidyButton.IsEnabled = ready;
        RequestButton.IsEnabled = ready;

        var cloud = ready && _assistant.SendsTextOffMachine;
        CheckCloud.IsVisible = cloud;
        TidyCloud.IsVisible = cloud;
        RequestCloud.IsVisible = cloud;

        AiHint.Text = why ?? (cloud ? "Buttons with the cloud mark send the draft to Anthropic." : string.Empty);
        AiHint.IsVisible = AiHint.Text.Length > 0;
    }

    private void OnCheckMyThinking(object? sender, RoutedEventArgs e) => _ = StartAiAsync(AssistantAction.CheckMyThinking);

    /// <summary>Runs an action as if its button had been pressed. For the developer switches.</summary>
    public void RunAction(AssistantAction action) => _ = StartAiAsync(action);

    private void OnTidy(object? sender, RoutedEventArgs e) => _ = StartAiAsync(AssistantAction.Tidy);

    private void OnMakeARequest(object? sender, RoutedEventArgs e) => _ = StartAiAsync(AssistantAction.MakeARequest);

    private async Task StartAiAsync(AssistantAction action)
    {
        var text = Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            ShowPanel(action);
            ShowMessage("Nothing to read yet. Type something first.");
            return;
        }

        if (_assistant.SendsTextOffMachine && !_settings.Current.Ai.CloudConfirmed)
        {
            _pendingAction = action;
            ShowPanel(action);
            CloudConfirmText.Text =
                $"This will send your draft ({text.Length:N0} characters) and the instructions for “{AssistantActions.Title(action)}” " +
                "to Anthropic over an encrypted connection. Nothing else goes: no name, no other text, no history. The reply comes back here.";
            CloudDontAsk.IsChecked = false;
            CloudConfirm.IsVisible = true;
            return;
        }

        await RunAiAsync(action, text);
    }

    private void OnCloudConfirmed(object? sender, RoutedEventArgs e)
    {
        if (CloudDontAsk.IsChecked == true)
        {
            _settings.Update(s => s.Ai.CloudConfirmed = true);
        }

        CloudConfirm.IsVisible = false;
        if (_pendingAction is { } action)
        {
            _pendingAction = null;
            _ = RunAiAsync(action, Text);
        }
    }

    private async Task RunAiAsync(AssistantAction action, string text)
    {
        _aiRun?.Cancel();
        var run = new CancellationTokenSource();
        _aiRun = run;

        ShowPanel(action);
        Thinking.IsVisible = true;
        ThinkingText.Text = _assistant.SendsTextOffMachine
            ? "Asking Claude…"
            : "Thinking on this PC. The first time takes a while, as the model loads…";

        var streaming = !AssistantActions.ReturnsNotes(action);
        var progress = new Progress<string>(partialText =>
        {
            if (streaming && !run.IsCancellationRequested)
            {
                ResultText.Text = partialText;
                ResultText.IsVisible = true;
            }
        });

        try
        {
            var result = await _assistant.RunAsync(action, text, progress, run.Token);
            if (run.IsCancellationRequested)
            {
                return;
            }

            if (AssistantActions.ReturnsNotes(action))
            {
                var notes = NotesParser.Parse(result.Output, text, action);
                _notes.Clear();
                foreach (var note in notes)
                {
                    _notes.Add(note);
                }

                if (notes.Count == 0)
                {
                    ShowMessage(action == AssistantAction.Tidy
                        ? "Nothing to fix. The spelling and grammar look fine."
                        : "Nothing to note. It reads clearly.");
                }
            }
            else
            {
                _resultText = result.Output.Trim();
                ResultText.Text = _resultText;
                ResultText.IsVisible = true;
                ResultButtons.IsVisible = _resultText.Length > 0;
            }

            NotesSubtitle.Text = Subtitle(result);
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_aiRun, run))
            {
                HidePanel();
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Couldn't do that: {ex.Message}");
        }
        finally
        {
            Thinking.IsVisible = false;
            if (ReferenceEquals(_aiRun, run))
            {
                _aiRun = null;
            }

            run.Dispose();
        }
    }

    private string Subtitle(AssistantResult result)
    {
        if (!_assistant.SendsTextOffMachine || (result.InputTokens == 0 && result.OutputTokens == 0))
        {
            return _assistant.Name;
        }

        var model = _settings.Current.Ai.CloudModel;
        var cost = CloudCost.EstimateUsd(model, result.InputTokens, result.OutputTokens);
        return $"{_assistant.Name} · about {CloudCost.Describe(cost)}";
    }

    private void ShowPanel(AssistantAction action)
    {
        NotesPanel.IsVisible = true;
        NotesTitle.Text = AssistantActions.Title(action);
        NotesSubtitle.Text = _assistant.Name;
        _notes.Clear();
        _resultText = string.Empty;
        NotesMessage.IsVisible = false;
        ResultText.Text = string.Empty;
        ResultText.IsVisible = false;
        ResultButtons.IsVisible = false;
        CloudConfirm.IsVisible = false;
        Thinking.IsVisible = false;
    }

    private void ShowMessage(string text)
    {
        NotesMessage.Text = text;
        NotesMessage.IsVisible = true;
    }

    private void HidePanel()
    {
        NotesPanel.IsVisible = false;
        _notes.Clear();
    }

    private void OnCloseNotes(object? sender, RoutedEventArgs e)
    {
        _aiRun?.Cancel();
        _pendingAction = null;
        HidePanel();
    }

    private void OnCancelAi(object? sender, RoutedEventArgs e) => _aiRun?.Cancel();

    /// <summary>Clicking a note shows where it points.</summary>
    private void OnNotePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border { DataContext: WritingNote note } || !note.IsAnchored || note.End > Text.Length)
        {
            return;
        }

        Editor.CaretIndex = note.End;
        Editor.SelectionStart = note.Start;
        Editor.SelectionEnd = note.End;
        Editor.Focus();
    }

    /// <summary>Applies one note's fix through the editor's own path, so Ctrl+Z reverses it, then moves the other notes along.</summary>
    private void OnApplyNote(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: WritingNote note } || !note.HasFix)
        {
            return;
        }

        e.Handled = true;
        var text = Text;
        var (start, length) = note.IsAnchored && note.End <= text.Length
            ? (note.Start, note.Length)
            : SpanAnchor.Find(text, note.Span, 0);
        if (start < 0)
        {
            ShowMessage("Couldn't find those words in the draft any more.");
            return;
        }

        var fix = note.Fix!;
        Editor.CaretIndex = start;
        Editor.SelectionStart = start;
        Editor.SelectionEnd = start + length;
        Editor.SelectedText = fix;

        // The text-changed handler has already shifted the other notes; just drop this one.
        var index = _notes.IndexOf(note);
        if (index >= 0)
        {
            _notes.RemoveAt(index);
        }

        if (_notes.Count == 0)
        {
            ShowMessage("All done.");
        }

        Editor.CaretIndex = start + fix.Length;
        CheckNow();
    }

    private void ShiftNotes(string oldText, string newText)
    {
        for (var i = 0; i < _notes.Count; i++)
        {
            var note = _notes[i];
            if (!note.IsAnchored)
            {
                continue;
            }

            var (start, length) = SpellingErrors.ShiftRange(oldText, newText, note.Start, note.Length);
            if (start != note.Start || length != note.Length)
            {
                _notes[i] = note with { Start = start, Length = length };
            }
        }
    }

    private void OnUseResult(object? sender, RoutedEventArgs e)
    {
        if (_resultText.Length == 0)
        {
            return;
        }

        Editor.SelectAll();
        Editor.SelectedText = _resultText;
        Editor.CaretIndex = _resultText.Length;
        HidePanel();
        CheckNow();
    }

    private void OnCopyResult(object? sender, RoutedEventArgs e) => CopyRequested?.Invoke(_resultText);

    private void OnReadResult(object? sender, RoutedEventArgs e) => ReadBackRequested?.Invoke(_resultText);

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
