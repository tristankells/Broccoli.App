using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Broccoli.Avalonia.Shell;
using Broccoli.Avalonia.Slices.Settings.Sync;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        // Composition root: build the DI container once and make it available app-wide via
        // CommunityToolkit.Mvvm's Ioc.Default, so view models declare their dependencies through
        // constructor injection instead of `new`-ing services/other view models directly.
        var services = new ServiceCollection();
        services.AddAppServices();
        var provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Ensure the local SQLite database exists and is on the latest schema before
            // anything (including sync) touches it.
            using (var db = Storage.BroccoliDbContext.CreateForApp())
            {
                db.Database.Migrate();
            }

            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // Resolved directly (not via MainViewModel.SettingsViewModel) so background sync
            // works at startup/shutdown regardless of whether the user ever opens the settings
            // flyout - that lazily-constructed view model is purely a presentation concern.
            var syncService = provider.GetRequiredService<IGoogleDriveSyncService>();

            // Best-effort: check Drive for changes made on other devices and auto-pull if safe,
            // without blocking startup. No-ops silently if Drive backup isn't connected.
            _ = syncService.SyncAsync();

            desktop.ShutdownRequested += (_, _) =>
            {
                // Best-effort push of local changes; intentionally fire-and-forget so app close
                // is never blocked/delayed waiting on network. If it doesn't finish in time, the
                // next startup's sync will simply pick these changes up then.
                _ = syncService.PushOnlyAsync();
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = provider.GetRequiredService<MainViewModel>() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}