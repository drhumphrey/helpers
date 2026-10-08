using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Helpers.Core.Capture;

namespace Helpers.Windows;

/// <summary>
/// Reads the selected text from the app in front. UI Automation first,
/// time-boxed, because Chromium and Electron build their trees lazily.
/// Then the clipboard: save it all, clear it, send Ctrl+Insert, wait,
/// put it all back. Ctrl+C only as a last resort and never to a terminal,
/// where it means "interrupt".
/// </summary>
public sealed class SelectionCapture : ISelectionSource
{
    private static readonly TimeSpan UiaBudget = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan CtrlInsertWait = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan CtrlCWait = TimeSpan.FromMilliseconds(1000);

    private static readonly HashSet<string> TerminalProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "WindowsTerminal", "conhost", "cmd", "powershell", "pwsh", "Code", "Code - Insiders", "Cursor", "wt", "mintty", "alacritty", "wezterm-gui",
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<SelectionResult> CaptureAsync(CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return SelectionResult.Failed("Already capturing.");
        }

        try
        {
            var hwnd = ForegroundWindow.Handle;
            var process = ForegroundWindow.ProcessName(hwnd);
            var processId = ForegroundWindow.ProcessId(hwnd);

            if (processId != 0 && !ProcessElevation.SelfIsElevated() && ProcessElevation.IsElevated(processId))
            {
                return SelectionResult.Elevated();
            }

            var uia = await TryUiAutomationAsync(cancellationToken).ConfigureAwait(false);
            if (uia is not null)
            {
                return uia;
            }

            return await TryClipboardAsync(TerminalProcesses.Contains(process), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Asks the focused element for its selection. Null means "no answer in time", so try the clipboard.</summary>
    private static async Task<SelectionResult?> TryUiAutomationAsync(CancellationToken cancellationToken)
    {
        var work = Task.Run(() =>
        {
            using var automation = new UIA3Automation();
            var focused = automation.FocusedElement();
            if (focused is null)
            {
                return null;
            }

            if (focused.Properties.IsPassword.ValueOrDefault)
            {
                return SelectionResult.Password();
            }

            var pattern = focused.Patterns.Text.PatternOrDefault;
            if (pattern is null)
            {
                return null;
            }

            var ranges = pattern.GetSelection();
            if (ranges is null || ranges.Length == 0)
            {
                return null;
            }

            var text = string.Concat(ranges.Select(r => r.GetText(-1)));
            return string.IsNullOrWhiteSpace(text) ? null : SelectionResult.Found(text, "UI Automation");
        }, cancellationToken);

        var finished = await Task.WhenAny(work, Task.Delay(UiaBudget, cancellationToken)).ConfigureAwait(false);
        if (finished != work)
        {
            return null;
        }

        try
        {
            return await work.ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<SelectionResult> TryClipboardAsync(bool terminal, CancellationToken cancellationToken)
    {
        // Wait for the user to let go of the shortcut's modifier keys, or the copy chord gets mixed with them.
        var patience = Stopwatch.StartNew();
        while (KeyboardInput.ModifiersHeld() && patience.ElapsedMilliseconds < 1500)
        {
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }

        var snapshot = ClipboardSnapshot.Take();
        try
        {
            ClipboardSnapshot.Clear();
            var before = ClipboardText.SequenceNumber;

            KeyboardInput.SendCtrlInsert();
            var text = await WaitForTextAsync(before, CtrlInsertWait, cancellationToken).ConfigureAwait(false);

            if (text is null && !terminal)
            {
                KeyboardInput.SendCtrlC();
                text = await WaitForTextAsync(before, CtrlCWait, cancellationToken).ConfigureAwait(false);
            }

            return string.IsNullOrWhiteSpace(text)
                ? SelectionResult.Nothing()
                : SelectionResult.Found(text, terminal ? "clipboard, Ctrl+Insert" : "clipboard");
        }
        finally
        {
            snapshot.Restore();
        }
    }

    private static async Task<string?> WaitForTextAsync(uint sequenceBefore, TimeSpan budget, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < budget)
        {
            if (ClipboardText.SequenceNumber != sequenceBefore)
            {
                var text = ClipboardText.TryGet();
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }

            await Task.Delay(15, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }
}
