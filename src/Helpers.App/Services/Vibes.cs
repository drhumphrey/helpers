using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Helpers.Core.Settings;

namespace Helpers.App.Services;

/// <summary>Swaps the vibe resource dictionary at runtime. Every surface uses DynamicResource, so it changes live.</summary>
public static class Vibes
{
    public static void Apply(Application app, Vibe vibe, ThemeChoice theme)
    {
        var name = vibe == Vibe.Calm ? "Calm" : "Neon";
        var include = new ResourceInclude(new Uri("avares://Helpers.App/"))
        {
            Source = new Uri($"avares://Helpers.App/Themes/{name}.axaml"),
        };

        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(include);

        // Neon is designed for dark. Calm follows the user's choice.
        app.RequestedThemeVariant = vibe == Vibe.Calm
            ? theme switch
            {
                ThemeChoice.Light => ThemeVariant.Light,
                ThemeChoice.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            }
            : ThemeVariant.Dark;
    }
}
