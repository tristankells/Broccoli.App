using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Broccoli.Avalonia.Shell;
using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.Avalonia;

public partial class App : Application
{
    /// <summary>
    /// Set by a platform head (e.g. Android) before the app initializes, to supply its own
    /// Google OAuth client id and code receiver. Desktop leaves this null and falls back to
    /// <see cref="DesktopGoogleDriveOAuthPlatform"/>.
    /// </summary>
    public static IGoogleDriveOAuthPlatform? GoogleDriveOAuthPlatformOverride { get; set; }

    /// <summary>
    /// Set by a platform head (e.g. Android) before the app initializes to replace the entire
    /// Google Drive auth service with a platform-specific implementation. Android does this because
    /// it must use Google Identity Services' <c>AuthorizationClient</c> instead of the shared
    /// loopback/custom-scheme OAuth flow. Desktop/iOS leave this null.
    /// </summary>
    public static IGoogleDriveAuthService? GoogleDriveAuthServiceOverride { get; set; }

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
        services.AddSingleton<IGoogleDriveOAuthPlatform>(
            GoogleDriveOAuthPlatformOverride ?? new DesktopGoogleDriveOAuthPlatform());
        if (GoogleDriveAuthServiceOverride is { } authServiceOverride)
        {
            services.AddSingleton<IGoogleDriveAuthService>(authServiceOverride);
        }

        services.AddAppServices();
        ServiceProvider provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);

        MainViewModel mainViewModel = provider.GetRequiredService<MainViewModel>();
        IGoogleDriveSyncService? syncService = null;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            // Resolved directly (not via MainViewModel.SettingsViewModel) so background sync
            // works at startup/shutdown regardless of whether the user ever opens the settings
            // flyout - that lazily-constructed view model is purely a presentation concern.
            syncService = provider.GetRequiredService<IGoogleDriveSyncService>();

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
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = mainViewModel };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = mainViewModel,
            };
        }

        // The window/first view is now assigned; from here on Avalonia can show it. Everything
        // below runs off the UI thread after the first frame so the window appears immediately
        // and the UI is filled in as data becomes ready.
        base.OnFrameworkInitializationCompleted();

        _ = RunStartupAsync(mainViewModel, syncService);
    }

    private static async Task RunStartupAsync(MainViewModel mainViewModel, IGoogleDriveSyncService? syncService)
    {
        try
        {
            await MigrateDatabaseAsync();

            // Populate the initially-visible page (Recipes) once the schema is ready.
            await mainViewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            // Startup data loading is best-effort; never let it crash the just-shown window.
            System.Diagnostics.Trace.TraceError($"Startup initialization failed: {ex}");
        }

        // Best-effort: check Drive for changes made on other devices and auto-pull if safe.
        // No-ops silently if Drive backup isn't connected.
        if (syncService is not null)
        {
            _ = syncService.SyncAsync();
        }
    }

    /// <summary>
    /// Ensures the local SQLite database exists and is on the latest schema, off the UI thread.
    /// Must run on every platform before anything (including sync) touches the database.
    /// </summary>
    private static Task MigrateDatabaseAsync() => Task.Run(() =>
    {
        using var db = Storage.BroccoliDbContext.CreateForApp();
        db.Database.Migrate();
    });
}
