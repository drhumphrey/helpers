using System.Runtime.InteropServices;

namespace Helpers.Windows;

/// <summary>Windows accessibility preferences the app should honour.</summary>
public static class SystemPreferences
{
    private const uint SPI_GETCLIENTAREAANIMATION = 0x1042;

    /// <summary>True when the user has turned animations off in Windows' accessibility settings.</summary>
    public static bool ReducedMotion
    {
        get
        {
            var enabled = true;
            return SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref enabled, 0) && !enabled;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, ref bool value, uint flags);
}
