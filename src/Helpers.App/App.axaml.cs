using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Helpers.App.Spike;

namespace Helpers.App;

public partial class App : Application
{
    private TrayIcon? _tray;
    private FocusSpikeWindow? _spike;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // There is no main window. The app lives in the tray and shows
            // small surfaces when it has something to do.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _tray = BuildTrayIcon(desktop);
            TrayIcon.SetIcons(this, [_tray]);

            ShowSpike();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private TrayIcon BuildTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var showSpike = new NativeMenuItem("Show focus spike");
        showSpike.Click += (_, _) => ShowSpike();

        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) => desktop.Shutdown();

        var menu = new NativeMenu();
        menu.Items.Add(showSpike);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exit);

        return new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Helpers.App/Assets/tray.png"))),
            ToolTipText = "Helpers",
            Menu = menu,
            IsVisible = true,
        };
    }

    private void ShowSpike()
    {
        if (_spike is null)
        {
            _spike = new FocusSpikeWindow();
            _spike.Closed += (_, _) => _spike = null;
        }

        _spike.Show();
    }
}
