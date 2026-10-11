using Helpers.Core;
using Microsoft.Win32;

namespace Helpers.Windows;

/// <summary>Start with Windows, through the per-user Run key. No admin rights needed.</summary>
public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = HelpersInfo.Name;

    /// <summary>
    /// A start-up entry left under the working title, Helpers, points at a program that may be gone.
    /// Swap it for one under the current name that starts this copy. Safe to run at every start.
    /// </summary>
    public static void MovePrevious()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key?.GetValue(HelpersInfo.PreviousName) is not string value || value.Length == 0)
            {
                return;
            }

            key.DeleteValue(HelpersInfo.PreviousName, throwOnMissingValue: false);
            if (!IsEnabled())
            {
                SetEnabled(true);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            // Left for the next start.
        }
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is string value && value.Length > 0;
    }

    /// <summary>Registers or removes the current executable. Returns false if the registry refused.</summary>
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (key is null)
            {
                return false;
            }

            if (enabled)
            {
                var exe = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe))
                {
                    return false;
                }

                key.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return false;
        }
    }
}
