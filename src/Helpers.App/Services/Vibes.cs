using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Helpers.Core.Settings;

namespace Helpers.App.Services;

/// <summary>
/// Swaps the vibe resource dictionary at runtime. Neon and Calm come from
/// XAML; Custom is built in code from the user's colours. Every surface uses
/// DynamicResource, so the change is live.
/// </summary>
public static class Vibes
{
    /// <summary>Named starting points for the Custom vibe.</summary>
    public static readonly IReadOnlyList<(string Name, string[] Colours)> Palettes =
    [
        ("Neon", ["#19E6FF", "#FF2FD1", "#FFE14D"]),
        ("Aurora", ["#19FFB0", "#8A7CFF"]),
        ("Sunset", ["#FF8A5B", "#FF2FD1", "#7A5CFF"]),
        ("Ocean", ["#19E6FF", "#2F6BFF"]),
        ("Lime", ["#B6FF3B", "#19E6FF"]),
        ("Candy", ["#FF7AE0", "#FFE14D"]),
    ];

    public static void Apply(Application app, AppSettings settings)
    {
        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(settings.Vibe switch
        {
            Vibe.Calm => Include("Calm"),
            Vibe.Custom => BuildGradientDictionary(settings.GradientColours),
            _ => Include("Neon"),
        });

        // Gradient vibes are designed for dark. Calm follows the user's choice.
        app.RequestedThemeVariant = settings.Vibe == Vibe.Calm
            ? settings.Theme switch
            {
                ThemeChoice.Light => ThemeVariant.Light,
                ThemeChoice.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            }
            : ThemeVariant.Dark;
    }

    /// <summary>Parses the user's colours, falling back to Neon's if fewer than two are valid.</summary>
    public static Color[] ParseColours(IEnumerable<string> hexes)
    {
        var colours = new List<Color>();
        foreach (var hex in hexes)
        {
            if (Color.TryParse(hex?.Trim() ?? string.Empty, out var colour))
            {
                colours.Add(colour);
            }
        }

        if (colours.Count < 2)
        {
            return Palettes[0].Colours.Select(Color.Parse).ToArray();
        }

        return colours.Take(3).ToArray();
    }

    private static ResourceInclude Include(string name) =>
        new(new Uri("avares://Helpers.App/"))
        {
            Source = new Uri($"avares://Helpers.App/Themes/{name}.axaml"),
        };

    private static ResourceDictionary BuildGradientDictionary(IEnumerable<string> hexes)
    {
        var colours = ParseColours(hexes);
        var first = colours[0];
        var second = colours[1];
        var mid = colours.Length > 2 ? colours[1] : Mix(first, second);
        var onGradient = Luminance(mid) > 0.45 ? Color.Parse("#0B0D12") : Colors.White;

        return new ResourceDictionary
        {
            ["OverlaySurfaceBrush"] = new SolidColorBrush(Color.Parse("#F5161821")),
            ["OverlayBorderBrush"] = Gradient(colours, 255, diagonal: true),
            ["OverlayGlow"] = BoxShadows.Parse(
                $"0 0 26 0 {Hex(first, 0x42)}, 0 0 60 0 {Hex(second, 0x1F)}, 0 12 32 0 #8C000000"),
            ["PrimaryBrush"] = Gradient([first, colours.Length > 2 ? colours[1] : second], 255, diagonal: true),
            ["PrimaryForegroundBrush"] = new SolidColorBrush(onGradient),
            ["HighlightBrush"] = Gradient([first, second], 0x3D, diagonal: false),
            ["HighlightForegroundBrush"] = new SolidColorBrush(Colors.White),
            ["WordBrush"] = new SolidColorBrush(Color.Parse("#4DFFFFFF")),
            ["TextBrush"] = new SolidColorBrush(Color.Parse("#F2F4F8")),
            ["MutedBrush"] = new SolidColorBrush(Color.Parse("#A9B1BD")),
            ["DoneBrush"] = new SolidColorBrush(Color.Parse("#7E8794")),
            ["LineBrush"] = new SolidColorBrush(Color.Parse("#24FFFFFF")),
            ["AccentTextBrush"] = new SolidColorBrush(first),
            ["SuccessBrush"] = new SolidColorBrush(Color.Parse("#6EDFA3")),
            ["ErrorBrush"] = new SolidColorBrush(Color.Parse("#FF8A80")),
        };
    }

    private static LinearGradientBrush Gradient(IReadOnlyList<Color> colours, byte alpha, bool diagonal)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, diagonal ? 1 : 0, RelativeUnit.Relative),
        };

        for (var i = 0; i < colours.Count; i++)
        {
            var offset = colours.Count == 1 ? 0 : (double)i / (colours.Count - 1);
            if (colours.Count == 3 && i == 1)
            {
                offset = 0.55;
            }

            var colour = colours[i];
            brush.GradientStops.Add(new GradientStop(new Color(alpha, colour.R, colour.G, colour.B), offset));
        }

        return brush;
    }

    private static string Hex(Color colour, byte alpha) => $"#{alpha:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";

    private static Color Mix(Color a, Color b) =>
        new(255, (byte)((a.R + b.R) / 2), (byte)((a.G + b.G) / 2), (byte)((a.B + b.B) / 2));

    private static double Luminance(Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
}
