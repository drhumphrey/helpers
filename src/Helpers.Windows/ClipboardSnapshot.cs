using System.Runtime.InteropServices;

namespace Helpers.Windows;

/// <summary>
/// Saves everything on the clipboard so it can be put back after a copy
/// is borrowed for selection capture. The user's clipboard must never end
/// up changed. Formats that can't be copied out (bitmaps, metafiles,
/// app-private ones) are skipped; the equivalent DIB usually carries the image.
/// </summary>
public sealed class ClipboardSnapshot
{
    private const uint CF_BITMAP = 2;
    private const uint CF_METAFILEPICT = 3;
    private const uint CF_ENHMETAFILE = 14;
    private const uint CF_OWNERDISPLAY = 0x0080;
    private const uint CF_DSPTEXT = 0x0081;
    private const uint CF_DSPBITMAP = 0x0082;
    private const uint CF_DSPMETAFILEPICT = 0x0083;
    private const uint CF_DSPENHMETAFILE = 0x008E;
    private const uint CF_PRIVATEFIRST = 0x0200;
    private const uint CF_PRIVATELAST = 0x02FF;
    private const uint CF_GDIOBJFIRST = 0x0300;
    private const uint CF_GDIOBJLAST = 0x03FF;
    private const uint GMEM_MOVEABLE = 0x0002;

    private readonly List<(uint Format, byte[] Data)> _items = [];

    private ClipboardSnapshot()
    {
    }

    public int FormatCount => _items.Count;

    /// <summary>Copies every copyable format off the clipboard. Returns an empty snapshot if the clipboard is busy.</summary>
    public static ClipboardSnapshot Take()
    {
        var snapshot = new ClipboardSnapshot();
        if (!TryOpen())
        {
            return snapshot;
        }

        try
        {
            uint format = 0;
            while ((format = EnumClipboardFormats(format)) != 0)
            {
                if (!IsCopyable(format))
                {
                    continue;
                }

                var handle = GetClipboardData(format);
                if (handle == 0)
                {
                    continue;
                }

                var size = GlobalSize(handle);
                if (size == 0)
                {
                    continue;
                }

                var pointer = GlobalLock(handle);
                if (pointer == 0)
                {
                    continue;
                }

                try
                {
                    var data = new byte[size];
                    Marshal.Copy(pointer, data, 0, (int)size);
                    snapshot._items.Add((format, data));
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
        }
        finally
        {
            CloseClipboard();
        }

        return snapshot;
    }

    /// <summary>Empties the clipboard so a fresh copy can be detected.</summary>
    public static bool Clear()
    {
        if (!TryOpen())
        {
            return false;
        }

        try
        {
            return EmptyClipboard();
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>Puts the saved formats back. An empty snapshot leaves the clipboard empty, as it was.</summary>
    public bool Restore()
    {
        if (!TryOpen())
        {
            return false;
        }

        try
        {
            EmptyClipboard();
            foreach (var (format, data) in _items)
            {
                var handle = GlobalAlloc(GMEM_MOVEABLE, (nuint)data.Length);
                if (handle == 0)
                {
                    continue;
                }

                var pointer = GlobalLock(handle);
                if (pointer == 0)
                {
                    GlobalFree(handle);
                    continue;
                }

                Marshal.Copy(data, 0, pointer, data.Length);
                GlobalUnlock(handle);
                if (SetClipboardData(format, handle) == 0)
                {
                    GlobalFree(handle);
                }
            }

            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static bool IsCopyable(uint format)
    {
        if (format is CF_BITMAP or CF_METAFILEPICT or CF_ENHMETAFILE or CF_OWNERDISPLAY
            or CF_DSPTEXT or CF_DSPBITMAP or CF_DSPMETAFILEPICT or CF_DSPENHMETAFILE)
        {
            return false;
        }

        if (format is >= CF_PRIVATEFIRST and <= CF_PRIVATELAST || format is >= CF_GDIOBJFIRST and <= CF_GDIOBJLAST)
        {
            return false;
        }

        return true;
    }

    private static bool TryOpen()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            if (OpenClipboard(0))
            {
                return true;
            }

            Thread.Sleep(25);
        }

        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(nint owner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetClipboardData(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetClipboardData(uint format, nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalFree(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint GlobalSize(nint handle);
}
