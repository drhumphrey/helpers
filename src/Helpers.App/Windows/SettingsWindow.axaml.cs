using Avalonia.Controls;
using Avalonia.Interactivity;
using Helpers.App.ViewModels;

namespace Helpers.App.Windows;

/// <summary>A normal window; it takes focus because it holds controls to type into.</summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnPreview(object? sender, RoutedEventArgs e) => _viewModel.PreviewVoice();

    private void OnApplyColours(object? sender, RoutedEventArgs e) => _viewModel.ApplyColours();
}
