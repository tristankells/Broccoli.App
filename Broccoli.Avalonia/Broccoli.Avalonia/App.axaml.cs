using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Broccoli.Avalonia.ViewModels;
using Broccoli.Avalonia.Views;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Broccoli.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Ensure the local SQLite database exists and is on the latest schema before
            // anything (including sync) touches it.
            using (var db = Storage.BroccoliDbContext.CreateForApp())
            {
                db.Database.Migrate();
            }

            var mainViewModel = new MainViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // Best-effort: check Drive for changes made on other devices and auto-pull if safe,
            // without blocking startup. No-ops silently if Drive backup isn't connected.
            _ = mainViewModel.SettingsViewModel.SyncNowCommand.ExecuteAsync(null);

            desktop.ShutdownRequested += (_, _) =>
            {
                // Best-effort push of local changes; intentionally fire-and-forget so app close
                // is never blocked/delayed waiting on network. If it doesn't finish in time, the
                // next startup's sync will simply pick these changes up then.
                _ = mainViewModel.SettingsViewModel.SyncService.PushOnlyAsync();
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = new MainViewModel() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}