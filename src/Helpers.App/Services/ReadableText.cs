using Avalonia;
using Avalonia.Media;
using Helpers.Core.Settings;

namespace Helpers.App.Services;

/// <summary>
/// Publishes the readable-text settings as application resources, so every
/// surface that shows the user's own text follows them through DynamicResource.
/// </summary>
public static class ReadableText
{
    public const string FontKey = "ReadingFontFamily";
    public const string SizeKey = "ReadingFontSize";
    public const string LineHeightKey = "ReadingLineHeight";
    public const string HeadingSizeKey = "ReadingHeadingSize";
    public const string TintKey = "ReadingTintBrush";

    public static void Apply(Application app, AppSettings settings)
    {
        var size = Math.Clamp(settings.ReadingFontSize, 14, 32);
        var spacing = Math.Clamp(settings.ReadingLineSpacing, 1.1, 2.2);

        app.Resources[FontKey] = settings.ReadingFontChoice switch
        {
            ReadingFont.Lexend => new FontFamily("avares://Helpers.App/Assets/Fonts#Lexend"),
            ReadingFont.AtkinsonHyperlegible => new FontFamily("avares://Helpers.App/Assets/Fonts#Atkinson Hyperlegible"),
            _ => FontFamily.Default,
        };
        app.Resources[SizeKey] = size;
        app.Resources[LineHeightKey] = Math.Round(size * spacing);
        app.Resources[HeadingSizeKey] = Math.Round(size * 1.1);
        app.Resources[TintKey] = settings.ReadingTintChoice switch
        {
            ReadingTint.Cream => new SolidColorBrush(Color.Parse("#1AFFE0A0")),
            ReadingTint.Grey => new SolidColorBrush(Color.Parse("#14808080")),
            _ => Brushes.Transparent,
        };
    }
}
