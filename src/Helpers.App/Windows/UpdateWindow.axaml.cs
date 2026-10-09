using Avalonia.Interactivity;
using Helpers.App.Services;
using Helpers.Core.Updates;

namespace Helpers.App.Windows;

/// <summary>What's new, and the choice: update now, open the page, skip this version, or not now.</summary>
public partial class UpdateWindow : ShellWindow
{
    private ReleaseInfo? _release;

    public UpdateWindow()
    {
        InitializeComponent();
    }

    public event Action<ReleaseInfo>? UpdateRequested;

    public event Action<ReleaseInfo>? SkipRequested;

    public void Show(ReleaseInfo release, string currentVersion, bool installed)
    {
        _release = release;
        Headline.Text = $"Helpers {release.Version} is ready";
        Versions.Text = $"You have {currentVersion}.";
        Notes.Text = release.Notes.Length > 0 ? release.Notes : "See the release page for what changed.";
        UpdateButton.IsVisible = installed && release.Installer is not null;
        Method.Text = UpdateButton.IsVisible
            ? "Update now downloads the installer, checks it, closes Helpers, installs the new version quietly, and starts it again. About half a minute."
            : "This copy wasn't put here by the installer, so download the new version from the page and unzip it over this one.";

        if (!IsVisible)
        {
            base.Show();
        }

        Activate();
    }

    private void OnUpdate(object? sender, RoutedEventArgs e)
    {
        if (_release is not null)
        {
            UpdateRequested?.Invoke(_release);
        }

        Close();
    }

    private void OnOpenPage(object? sender, RoutedEventArgs e)
    {
        if (_release is not null)
        {
            Links.Open(_release.Page);
        }

        Close();
    }

    private void OnSkip(object? sender, RoutedEventArgs e)
    {
        if (_release is not null)
        {
            SkipRequested?.Invoke(_release);
        }

        Close();
    }

    private void OnNotNow(object? sender, RoutedEventArgs e) => Close();
}
