namespace Helpers.Windows;

/// <summary>Maps the key names used in settings ("Space", "F9", "A") to Windows virtual-key codes.</summary>
public static class VirtualKeys
{
    private static readonly Dictionary<string, uint> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Space"] = 0x20,
        ["Insert"] = 0x2D,
        ["Delete"] = 0x2E,
        ["Escape"] = 0x1B,
        ["Enter"] = 0x0D,
        ["Tab"] = 0x09,
        ["Backspace"] = 0x08,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["PageUp"] = 0x21,
        ["PageDown"] = 0x22,
        ["Up"] = 0x26,
        ["Down"] = 0x28,
        ["Left"] = 0x25,
        ["Right"] = 0x27,
        ["Pause"] = 0x13,
        ["ScrollLock"] = 0x91,
        ["NumLock"] = 0x90,
        ["CapsLock"] = 0x14,
        ["PrintScreen"] = 0x2C,
    };

    /// <summary>The virtual key for a name, or 0 if unknown.</summary>
    public static uint From(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        if (Named.TryGetValue(key, out var named))
        {
            return named;
        }

        if (key.Length == 1)
        {
            var c = char.ToUpperInvariant(key[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                return c;
            }
        }

        if (key.Length >= 2 && (key[0] is 'F' or 'f') && int.TryParse(key[1..], out var f) && f is >= 1 and <= 24)
        {
            return 0x70u + (uint)(f - 1);
        }

        if (key.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase) && key.Length == 7 && char.IsDigit(key[6]))
        {
            return 0x60u + (uint)(key[6] - '0');
        }

        return 0;
    }
}
