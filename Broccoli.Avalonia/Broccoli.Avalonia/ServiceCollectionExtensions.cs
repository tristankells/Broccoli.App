using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Shell;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.Avalonia;

/// <summary>
/// Registers every service and view model the app resolves through DI (via
/// <see cref="CommunityToolkit.Mvvm.DependencyInjection.Ioc"/>, configured in
/// <c>App.axaml.cs</c>). Views are intentionally NOT registered here — they have no
/// dependencies of their own and are created directly by <see cref="ViewLocator"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Services
        services.AddSingleton<IRecipeService, RecipeService>();
        services.AddSingleton<IGoogleDriveAuthService, GoogleDriveAuthService>();
        services.AddSingleton<IGoogleDriveSyncService, GoogleDriveSyncService>();
        services.AddSingleton<IMacroTargetService, MacroTargetService>();
        services.AddSingleton<MacroCalculatorService>();
        services.AddIngredientParsing();
        services.AddSingleton<IGroceryListService, GroceryListService>();
        services.AddSingleton<IngredientCartService>();

        // Nav page view models: registered as singletons so switching between Recipes/Planning/
        // Groceries and back preserves each page's in-progress state (e.g. Recipes' list/detail/
        // edit sub-navigation) instead of resetting it every time.
        services.AddSingleton<RecipesListViewModel>();
        services.AddSingleton<PlanningViewModel>();
        services.AddSingleton<GroceriesViewModel>();

        // SettingsViewModel is registered normally, but MainViewModel only ever depends on the
        // Lazy<T> wrapper below, so it isn't constructed - and doesn't touch the file system to
        // read the stored Drive account - until the user actually opens the settings flyout.
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton(serviceProvider => new Lazy<SettingsViewModel>(() => serviceProvider.GetRequiredService<SettingsViewModel>()));

        services.AddSingleton<MainViewModel>();

        return services;
    }
}
