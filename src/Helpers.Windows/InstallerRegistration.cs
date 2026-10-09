using Microsoft.Win32;

namespace Helpers.Windows;

/// <summary>Whether this copy was put here by the installer, which is what makes a silent in-place update possible.</summary>
public static class InstallerRegistration
{
    /// <summary>The AppId from installer/Helpers.iss, as Inno Setup writes it under the user's Uninstall key.</summary>
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{9E1D1F0E-6C1C-4E35-9B38-5B9C2B8E3F11}_is1";

    public static bool IsInstalled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(UninstallKey);
            return key is not null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
