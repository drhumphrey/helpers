using System.Runtime.InteropServices;

namespace Helpers.Windows;

/// <summary>Reads plain text from the Windows clipboard without touching anything else on it.</summary>
public static class ClipboardText
{
    private const uint CF_UNICODETEXT = 13;

    /// <summary>The text on the clipboard, or null if there is none or it is busy.</summary>
    public static string? TryGet()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (OpenClipboard(0))
            {
                try
                {
                    var handle = GetClipboardData(CF_UNICODETEXT);
                    if (handle == 0)
                    {
                        return null;
                    }

                    var pointer = GlobalLock(handle);
                    if (pointer == 0)
                    {
                        return null;
                    }

                    try
                    {
                        return Marshal.PtrToStringUni(pointer);
                    }
                    finally
                    {
                        GlobalUnlock(handle);
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }

            Thread.Sleep(30);
        }

        return null;
    }

    /// <summary>A number Windows bumps every time the clipboard changes.</summary>
    public static uint SequenceNumber => GetClipboardSequenceNumber();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(nint owner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetClipboardData(uint format);

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(nint handle);
}

/// <summary>Raises an event whenever the clipboard changes, by polling its sequence number.</summary>
public sealed class ClipboardWatcher : IDisposable
{
    private readonly Timer _timer;
    private uint _last;
    private bool _running;

    public ClipboardWatcher()
    {
        _last = ClipboardText.SequenceNumber;
        _timer = new Timer(_ => Poll(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Raised on a thread-pool thread. Marshal to the UI yourself.</summary>
    public event Action? Changed;

    public void Start()
    {
        _last = ClipboardText.SequenceNumber;
        _running = true;
        _timer.Change(400, 400);
    }

    public void Stop()
    {
        _running = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose() => _timer.Dispose();

    private void Poll()
    {
        if (!_running)
        {
            return;
        }

        var now = ClipboardText.SequenceNumber;
        if (now != _last)
        {
            _last = now;
            Changed?.Invoke();
        }
    }
}
