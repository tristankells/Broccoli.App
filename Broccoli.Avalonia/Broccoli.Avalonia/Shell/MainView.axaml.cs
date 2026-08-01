using Avalonia.Controls;

namespace Broccoli.Avalonia.Shell;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Defers setting the settings panel's DataContext until the flyout is actually opened for
    /// the first time, so <see cref="MainViewModel.SettingsViewModel"/> - and everything it
    /// touches (stored Drive account, pending conflicts) - isn't constructed at app startup.
    /// </summary>
    private void SettingsFlyout_Opening(object? sender, EventArgs e)
    {
        if (SettingsViewHost.DataContext is null && DataContext is MainViewModel viewModel)
        {
            SettingsViewHost.DataContext = viewModel.SettingsViewModel;
        }
    }
}