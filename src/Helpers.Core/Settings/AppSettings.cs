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

    /// <summary>Two or three hex colours for the Custom vibe's gradient.</summary>
    public List<string> GradientColours { get; set; } = ["#19E6FF", "#FF2FD1", "#FFE14D"];

    /// <summary>Let the gradient and highlight drift while reading.</summary>
    public bool AnimateWhileReading { get; set; } = true;

    /// <summary>How many flecks of colour drift around the player, 0 to 100. Calm ignores it.</summary>
    public int FleckDensity { get; set; } = 35;

    /// <summary>Scale for every overlay. 1.0 is the designed size.</summary>
    public double UiScale { get; set; } = 0.9;

    /// <summary>The reading view's last size, in device-independent pixels. Null means the default.</summary>
    public double? ReadingViewWidth { get; set; }

    public double? ReadingViewHeight { get; set; }

    /// <summary>Whether the player was left open in the reading view.</summary>
    public bool PlayerExpanded { get; set; }

    /// <summary>Read any new text that lands on the clipboard.</summary>
    public bool WatchClipboard { get; set; }

    /// <summary>The global shortcut that reads the current selection, or stops reading. Text such as "Ctrl+Alt+Space".</summary>
    public string ReadSelectionHotkey { get; set; } = "Ctrl+Alt+Space";

    public bool ReadSelectionHotkeyEnabled { get; set; } = true;

    /// <summary>Seconds after reading ends before the player hides. 0 keeps it open.</summary>
    public int PlayerHideAfterSeconds { get; set; } = 4;

    public WindowPlacement? PlayerPlacement { get; set; }

    /// <summary>Folder holding the voice models. Null means the default under local app data.</summary>
    public string? ModelsFolder { get; set; }

    public ReadingSettings Reading { get; set; } = new();
}
