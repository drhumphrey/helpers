using Helpers.Core.Text;

namespace Helpers.Core.Settings;

public enum Vibe
{
    Neon,
    Custom,
    Calm,
}

public enum ThemeChoice
{
    FollowOS,
    Light,
    Dark,
}

/// <summary>Where a window was last left, in screen pixels.</summary>
public sealed record WindowPlacement(int X, int Y);

/// <summary>Everything the user can change. Saved as JSON in the app's settings file.</summary>
public sealed class AppSettings
{
    public string VoiceId { get; set; } = "bm_george";

    /// <summary>0.5 to 2.0.</summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>0 to 1.</summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>The output device's name, or null for the OS default. Stored by name because device numbers change.</summary>
    public string? OutputDeviceName { get; set; }

    public Vibe Vibe { get; set; } = Vibe.Neon;

    public ThemeChoice Theme { get; set; } = ThemeChoice.FollowOS;

    /// <summary>Read any new text that lands on the clipboard.</summary>
    public bool WatchClipboard { get; set; }

    /// <summary>Seconds after reading ends before the player hides. 0 keeps it open.</summary>
    public int PlayerHideAfterSeconds { get; set; } = 4;

    public WindowPlacement? PlayerPlacement { get; set; }

    /// <summary>Folder holding the voice models. Null means the default under local app data.</summary>
    public string? ModelsFolder { get; set; }

    public ReadingSettings Reading { get; set; } = new();
}
