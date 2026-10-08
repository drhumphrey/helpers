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
    private HotkeyService? _hotkeys;
    private SelectionCapture? _selection;
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
            Vibes.Apply(this, settings);
            UiScale.Set(settings.UiScale);

            _toasts = new ToastService();
            _toastWindow = new ToastWindow(_toasts) { FollowFocus = settings.MessagesOn == MessageScreen.Focused };

            var modelsRoot = settings.ModelsFolder ?? SettingsStore.DefaultModelsFolder();
            var engine = new KokoroEngine(modelsRoot, Math.Clamp(Environment.ProcessorCount / 2, 2, 4));
            _reading = new ReadingController(engine, new NAudioOutput(), _settings, _toasts);

            _clipboard = new ClipboardWatcher();
            _clipboard.Changed += () => Dispatcher.UIThread.Post(OfferToReadClipboard);
            if (settings.WatchClipboard)
            {
                _clipboard.Start();
            }

            _reading.SettingsRequested += ShowSettings;

            _selection = new SelectionCapture();
            _hotkeys = new HotkeyService();
            _hotkeys.Pressed += () => _ = ReadSelectionAsync();
            ApplyHotkey(settings);

            _tray = BuildTrayIcon(desktop, settings);
            TrayIcon.SetIcons(this, [_tray]);

            desktop.Exit += (_, _) =>
            {
                _hotkeys?.Dispose();
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
            else if (args[i] == "--settings")
            {
                ShowSettings();
            }
            else if (args[i] == "--expanded")
            {
                _reading.ExpandPlayer();
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

        var settingsItem = new NativeMenuItem("Settings…");
        settingsItem.Click += (_, _) => ShowSettings();

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
        menu.Items.Add(settingsItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_calmItem);
        menu.Items.Add(memory);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exit);

        var tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Helpers.App/Assets/app.ico"))),
            ToolTipText = "Helpers",
            Menu = menu,
            IsVisible = true,
        };
        tray.Clicked += (_, _) => _reading?.ShowPlayer();
        return tray;
    }

    private Windows.SettingsWindow? _settingsWindow;

    /// <summary>
    /// Watch clipboard offers rather than reads: a copy during normal work must
    /// never start the voice by itself. The offer fades if ignored.
    /// </summary>
    private void OfferToReadClipboard()
    {
        if (_reading is null || _toasts is null)
        {
            return;
        }

        var text = ClipboardText.TryGet();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _toasts.Offer("Read what you just copied?", "Read", () => _reading.Read(text));
    }

    /// <summary>The shortcut's handler. The clipboard watcher sleeps while capture borrows the clipboard.</summary>
    private async Task ReadSelectionAsync()
    {
        if (_reading is null || _selection is null)
        {
            return;
        }

        _clipboard?.Suspend();
        try
        {
            await _reading.ReadSelectionOrStopAsync(_selection);
        }
        finally
        {
            _clipboard?.Resume();
        }
    }

    /// <summary>Registers the shortcut from settings and tells the user if Windows refused it.</summary>
    private bool ApplyHotkey(AppSettings settings)
    {
        if (_hotkeys is null)
        {
            return false;
        }

        var gesture = Helpers.Core.Input.HotkeyGesture.Parse(settings.ReadSelectionHotkey);
        var ok = _hotkeys.Apply(gesture, settings.ReadSelectionHotkeyEnabled);
        if (!ok)
        {
            _toasts?.Error($"Couldn't claim the shortcut {settings.ReadSelectionHotkey}. Another app may be using it.", "Open Settings", ShowSettings);
        }

        return ok;
    }

    private void ShowSettings()
    {
        if (_settings is null || _reading is null)
        {
            return;
        }

        if (_settingsWindow is null)
        {
            var viewModel = new ViewModels.SettingsViewModel(_settings, _reading, SetWatchClipboard, ApplyLook, () => ApplyHotkey(_settings.Current))
            {
                MessagesFollowFocusChanged = follow =>
                {
                    if (_toastWindow is not null)
                    {
                        _toastWindow.FollowFocus = follow;
                    }
                },
            };
            _settingsWindow = new Windows.SettingsWindow(viewModel);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private void ToggleWatchClipboard()
    {
        if (_settings is null)
        {
            return;
        }

        var on = !_settings.Current.WatchClipboard;
        _settings.Update(s => s.WatchClipboard = on);
        SetWatchClipboard(on);
    }

    private void SetWatchClipboard(bool on)
    {
        if (_clipboard is null)
        {
            return;
        }

        if (_watchItem is not null)
        {
            _watchItem.IsChecked = on;
        }

        if (on)
        {
            _clipboard.Start();
            _toasts?.Info("Watching the clipboard. Copy anything and you will be offered a Read button.");
        }
        else
        {
            _clipboard.Stop();
            _toasts?.Info("Stopped watching the clipboard");
        }
    }

    private void ToggleCalm()
    {
        if (_settings is null)
        {
            return;
        }

        var calm = _settings.Current.Vibe != Vibe.Calm;
        _settings.Update(s => s.Vibe = calm ? Vibe.Calm : Vibe.Neon);
        ApplyLook();
    }

    private void ApplyLook()
    {
        if (_settings is null)
        {
            return;
        }

        if (_calmItem is not null)
        {
            _calmItem.IsChecked = _settings.Current.Vibe == Vibe.Calm;
        }

        Vibes.Apply(this, _settings.Current);
    }
}
