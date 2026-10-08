using System.Runtime.InteropServices;

namespace Helpers.Windows;

/// <summary>Sends keystrokes to whatever app has focus. Used only for the copy shortcut during selection capture.</summary>
public static class KeyboardInput
{
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_INSERT = 0x2D;
    private const ushort VK_C = 0x43;
    private const ushort VK_MENU = 0x12;
    private const ushort VK_SHIFT = 0x10;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>Ctrl+Insert: copies in almost every app and never means "interrupt" in a terminal.</summary>
    public static void SendCtrlInsert() => SendChord(VK_CONTROL, VK_INSERT);

    /// <summary>Ctrl+C: the fallback for apps that ignore Ctrl+Insert. Never send to a terminal.</summary>
    public static void SendCtrlC() => SendChord(VK_CONTROL, VK_C);

    /// <summary>True while any of the shortcut modifier keys is physically held down.</summary>
    public static bool ModifiersHeld() =>
        (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 ||
        (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 ||
        (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

    private static void SendChord(ushort modifier, ushort key)
    {
        var inputs = new INPUT[4];
        inputs[0] = KeyInput(modifier, false);
        inputs[1] = KeyInput(key, false);
        inputs[2] = KeyInput(key, true);
        inputs[3] = KeyInput(modifier, true);
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT KeyInput(ushort key, bool up) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = key,
                dwFlags = up ? KEYEVENTF_KEYUP : 0,
            },
        },
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);
}
