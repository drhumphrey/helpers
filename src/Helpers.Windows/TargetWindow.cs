using System.Diagnostics;
using Helpers.Core.Capture;
using Helpers.Windows.Interop;

namespace Helpers.Windows;

/// <summary>
/// The window Compose sends to. Pasting: save the clipboard, put the text
/// on it, bring the window to the front, send Ctrl+V (Shift+Insert for
/// terminals), wait for the app to take it, put the clipboard back. Never Enter.
/// </summary>
public sealed class TargetWindow : ITargetWindow
{
    private static readonly TimeSpan FocusWait = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan PasteWait = TimeSpan.FromMilliseconds(500);

    private static readonly HashSet<string> ShellClasses = new(StringComparer.Ordinal)
    {
        "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Progman", "WorkerW", "NotifyIconOverflowWindow", "TopLevelWindowForOverflowXamlIsland",
    };

    private TargetWindow(nint handle, string appName, string title, uint processId, bool isTerminal)
    {
        Handle = handle;
        AppName = appName;
        Title = title;
        ProcessId = processId;
        IsTerminal = isTerminal;
    }

    public nint Handle { get; }

    public string AppName { get; }

    public string Title { get; }

    public uint ProcessId { get; }

    public bool IsTerminal { get; }

    public bool IsAlive => Handle != 0 && NativeMethods.IsWindow(Handle);

    /// <summary>
    /// The window in front right now, or null when that is this app, the
    /// taskbar or the desktop, none of which can be sent to.
    /// </summary>
    public static TargetWindow? FromForeground() => From(ForegroundWindow.Handle);

    public static TargetWindow? From(nint hwnd)
    {
        if (hwnd == 0 || !NativeMethods.IsWindow(hwnd))
        {
            return null;
        }

        var processId = ForegroundWindow.ProcessId(hwnd);
        if (processId == 0 || processId == (uint)Environment.ProcessId)
        {
            return null;
        }

        if (ShellClasses.Contains(ForegroundWindow.ClassName(hwnd)))
        {
            return null;
        }

        var process = ForegroundWindow.ProcessName(hwnd);
        var appName = ForegroundWindow.FriendlyName(hwnd);
        return new TargetWindow(hwnd, appName.Length > 0 ? appName : process, ForegroundWindow.Title(hwnd), processId, KnownApps.IsTerminal(process));
    }

    public async Task<PasteOutcome> PasteAsync(string text, CancellationToken cancellationToken)
    {
        if (!IsAlive)
        {
            return PasteOutcome.WindowGone;
        }

        if (!ProcessElevation.SelfIsElevated() && ProcessElevation.IsElevated(ProcessId))
        {
            return PasteOutcome.Elevated;
        }

        // Wait for the user to let go of any modifier, or the paste chord gets mixed with it.
        var patience = Stopwatch.StartNew();
        while (KeyboardInput.ModifiersHeld() && patience.ElapsedMilliseconds < 1500)
        {
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }

        var snapshot = ClipboardSnapshot.Take();
        if (!ClipboardText.TrySet(text))
        {
            return PasteOutcome.ClipboardBusy;
        }

        try
        {
            if (!await BringToFrontAsync(cancellationToken).ConfigureAwait(false))
            {
                return PasteOutcome.CouldNotFocus;
            }

            if (IsTerminal)
            {
                KeyboardInput.SendShiftInsert();
            }
            else
            {
                KeyboardInput.SendCtrlV();
            }

            await Task.Delay(PasteWait, cancellationToken).ConfigureAwait(false);
            return PasteOutcome.Pasted;
        }
        finally
        {
            snapshot.Restore();
        }
    }

    private async Task<bool> BringToFrontAsync(CancellationToken cancellationToken)
    {
        if (NativeMethods.IsIconic(Handle))
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SW_RESTORE);
        }

        NativeMethods.SetForegroundWindow(Handle);

        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < FocusWait)
        {
            if (NativeMethods.GetForegroundWindow() == Handle)
            {
                // A little settling time so the app's own focus lands in its text box.
                await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                return true;
            }

            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
