using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Helpers.Windows;

/// <summary>
/// A hidden, message-only window on its own thread. Global hotkeys are
/// registered here, and later the low-level mouse hook, because both need a
/// thread with a message loop that is never busy drawing UI.
/// </summary>
public sealed class MessageWindow : IDisposable
{
    private const uint WM_HOTKEY = 0x0312;
    private const uint WM_APP_RUN = 0x8000 + 1;
    private const uint WM_CLOSE = 0x0010;
    private const uint WM_DESTROY = 0x0002;
    private static readonly nint HWND_MESSAGE = -3;

    private readonly ConcurrentQueue<Action> _queued = new();
    private readonly ManualResetEventSlim _ready = new();
    private readonly WndProc _wndProc;
    private readonly string _className = "HelpersMessageWindow_" + Guid.NewGuid().ToString("N");
    private Thread? _thread;
    private nint _hwnd;
    private int _nextHotkeyId = 1;

    public MessageWindow()
    {
        _wndProc = WindowProcedure;
    }

    private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

    /// <summary>Raised on the message thread with the id returned by <see cref="RegisterHotkey"/>.</summary>
    public event Action<int>? HotkeyPressed;

    public nint Handle => _hwnd;

    public void Start()
    {
        if (_thread is not null)
        {
            return;
        }

        _thread = new Thread(Run) { IsBackground = true, Name = "Helpers message window" };
        _thread.Start();
        _ready.Wait(TimeSpan.FromSeconds(5));
    }

    /// <summary>Registers a global hotkey. Returns its id, or -1 if Windows refused (another app owns it).</summary>
    public int RegisterHotkey(bool ctrl, bool alt, bool shift, bool win, uint virtualKey)
    {
        var id = Interlocked.Increment(ref _nextHotkeyId);
        var modifiers = (ctrl ? 0x0002u : 0) | (alt ? 0x0001u : 0) | (shift ? 0x0004u : 0) | (win ? 0x0008u : 0) | 0x4000u;
        var ok = false;
        Invoke(() => ok = RegisterHotKey(_hwnd, id, modifiers, virtualKey));
        return ok ? id : -1;
    }

    public void UnregisterHotkey(int id)
    {
        if (id > 0)
        {
            Invoke(() => UnregisterHotKey(_hwnd, id));
        }
    }

    /// <summary>Runs an action on the message thread and waits for it.</summary>
    public void Invoke(Action action)
    {
        if (_hwnd == 0)
        {
            throw new InvalidOperationException("The message window has not started.");
        }

        if (Thread.CurrentThread == _thread)
        {
            action();
            return;
        }

        using var done = new ManualResetEventSlim();
        Exception? error = null;
        _queued.Enqueue(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        });

        PostMessage(_hwnd, WM_APP_RUN, 0, 0);
        done.Wait(TimeSpan.FromSeconds(5));
        if (error is not null)
        {
            throw new InvalidOperationException("The message-thread action failed.", error);
        }
    }

    public void Dispose()
    {
        if (_hwnd != 0)
        {
            PostMessage(_hwnd, WM_CLOSE, 0, 0);
        }

        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
    }

    private void Run()
    {
        var instance = GetModuleHandle(null);
        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = instance,
            lpszClassName = _className,
        };

        if (RegisterClassEx(ref wc) == 0)
        {
            _ready.Set();
            return;
        }

        _hwnd = CreateWindowEx(0, _className, "Helpers", 0, 0, 0, 0, 0, HWND_MESSAGE, 0, instance, 0);
        _ready.Set();
        if (_hwnd == 0)
        {
            return;
        }

        while (GetMessage(out var msg, 0, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        _hwnd = 0;
        UnregisterClass(_className, instance);
    }

    private nint WindowProcedure(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_HOTKEY:
                HotkeyPressed?.Invoke((int)wParam);
                return 0;
            case WM_APP_RUN:
                while (_queued.TryDequeue(out var action))
                {
                    action();
                }

                return 0;
            case WM_CLOSE:
                DestroyWindow(hWnd);
                return 0;
            case WM_DESTROY:
                PostQuitMessage(0);
                return 0;
            default:
                return DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public int x;
        public int y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool UnregisterClass(string className, nint instance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG msg, nint hWnd, uint filterMin, uint filterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG msg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessage(ref MSG msg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? moduleName);
}
