using System.Diagnostics;

namespace Helpers.App.Services;

/// <summary>Opens a web address in the user's browser. Only https addresses, so nothing else can be launched by mistake.</summary>
public static class Links
{
    public static bool Open(string url)
    {
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    public static string GoogleDefine(string word) => "https://www.google.com/search?q=" + Uri.EscapeDataString("define " + word);

    public static string Wiktionary(string word) => "https://en.wiktionary.org/wiki/" + Uri.EscapeDataString(word.ToLowerInvariant());
}
