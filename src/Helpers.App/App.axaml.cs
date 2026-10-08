using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Helpers.App.Overlays;
using Helpers.App.Services;
using Helpers.Core.Settings;
using Helpers.Speech;
using Helpers.Windows;

namespace Helpers.App;

public partial class App : Application
{
    private SettingsStore? _settings;
    private ToastService? _toasts;
    private ToastWindow? _toastWindow;
    private ReadingController? _reading;
    private ClipboardWatcher? _clipboard;
    private TrayIcon? _tray;
    private NativeMenuItem? _watchItem;
    private NativeMenuItem? _calmItem;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // No main window: the app lives in the tray and shows small surfaces when needed.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _settings = new SettingsStore();
            var settings = _settings.Load();
            Vibes.Apply(this, settings.Vibe, settings.Theme);

            _toasts = new ToastService();
            _toastWindow = new ToastWindow(_toasts);

            var modelsRoot = settings.ModelsFolder ?? SettingsStore.DefaultModelsFolder();
            var engine = new KokoroEngine(modelsRoot, Math.Clamp(Environment.ProcessorCount / 2, 2, 4));
            _reading = new ReadingController(engine, new NAudioOutput(), _settings, _toasts);

            _clipboard = new ClipboardWatcher();
            _clipboard.Changed += () => Dispatcher.UIThread.Post(() => _reading?.ReadClipboard());
            if (settings.WatchClipboard)
            {
                _clipboard.Start();
            }

            _tray = BuildTrayIcon(desktop, settings);
            TrayIcon.SetIcons(this, [_tray]);

            desktop.Exit += (_, _) =>
            {
                _clipboard?.Dispose();
                _reading?.Dispose();
            };

            _ = StartAsync(desktop.Args ?? []);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartAsync(string[] args)
    {
        if (_reading is null)
        {
            return;
        }

        await _reading.WarmUpAsync();

        // Developer switches: --read-file <path> reads a file at start-up; --read-clipboard reads the clipboard.
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--read-file" && i + 1 < args.Length && File.Exists(args[i + 1]))
            {
                _reading.Read(await File.ReadAllTextAsync(args[i + 1]));
            }
            else if (args[i] == "--read-clipboard")
            {
                _reading.ReadClipboard();
            }
        }
    }

    private TrayIcon BuildTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, AppSettings settings)
    {
        var menu = new NativeMenu();

        var readClipboard = new NativeMenuItem("Read clipboard");
        readClipboard.Click += (_, _) => _reading?.ReadClipboard();

        _watchItem = new NativeMenuItem("Watch clipboard") { ToggleType = MenuItemToggleType.CheckBox, IsChecked = settings.WatchClipboard };
        _watchItem.Click += (_, _) => ToggleWatchClipboard();

        var pause = new NativeMenuItem("Pause / Resume");
        pause.Click += (_, _) => _reading?.TogglePause();

        var stop = new NativeMenuItem("Stop");
        stop.Click += (_, _) => _reading?.Stop();

        var showPlayer = new NativeMenuItem("Show player");
        showPlayer.Click += (_, _) => _reading?.ShowPlayer();

        _calmItem = new NativeMenuItem("Calm look") { ToggleType = MenuItemToggleType.CheckBox, IsChecked = settings.Vibe == Vibe.Calm };
        _calmItem.Click += (_, _) => ToggleCalm();

        var memory = new NativeMenuItem("Memory in use");
        memory.Click += (_, _) =>
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            _toasts?.Info($"Working set {process.WorkingSet64 / 1_048_576:N0} MB, private {process.PrivateMemorySize64 / 1_048_576:N0} MB");
        };

        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) => desktop.Shutdown();

        menu.Items.Add(readClipboard);
        menu.Items.Add(_watchItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(pause);
        menu.Items.Add(stop);
        menu.Items.Add(showPlayer);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_calmItem);
        menu.Items.Add(memory);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exit);

        var tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Helpers.App/Assets/tray.png"))),
            ToolTipText = "Helpers",
            Menu = menu,
            IsVisible = true,
        };
        tray.Clicked += (_, _) => _reading?.ShowPlayer();
        return tray;
    }

    private void ToggleWatchClipboard()
    {
        if (_settings is null || _clipboard is null || _watchItem is null)
        {
            return;
        }

        var on = !_settings.Current.WatchClipboard;
        _settings.Update(s => s.WatchClipboard = on);
        _watchItem.IsChecked = on;
        if (on)
        {
            _clipboard.Start();
            _toasts?.Info("Watching the clipboard. Copy anything and it will be read.");
        }
        else
        {
            _clipboard.Stop();
            _toasts?.Info("Stopped watching the clipboard");
        }
    }

    private void ToggleCalm()
    {
        if (_settings is null || _calmItem is null)
        {
            return;
        }

        var calm = _settings.Current.Vibe != Vibe.Calm;
        _settings.Update(s => s.Vibe = calm ? Vibe.Calm : Vibe.Neon);
        _calmItem.IsChecked = calm;
        Vibes.Apply(this, _settings.Current.Vibe, _settings.Current.Theme);
    }
}
