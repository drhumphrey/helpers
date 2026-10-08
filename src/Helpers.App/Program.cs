using Avalonia;
using Avalonia.Media;

namespace Helpers.App;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .With(new FontManagerOptions
            {
                // Lexend, bundled under Assets/Fonts (SIL Open Font Licence), for all app text.
                DefaultFamilyName = "avares://Helpers.App/Assets/Fonts#Lexend",
                FontFallbacks = [new FontFallback { FontFamily = new FontFamily("Segoe UI") }],
            })
            .LogToTrace();
}
