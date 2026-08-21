using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shell;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Pantry;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Slices.Recipes.Import;
using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using Broccoli.Avalonia.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddSingleton<IRecipeHistoryStore, RecipeHistoryStore>();
        services.AddSingleton<IRecipeService, RecipeService>();
        services.TryAddSingleton<IGoogleDriveAuthService, GoogleDriveAuthService>();
        services.AddSingleton<IGoogleDriveSyncService, GoogleDriveSyncService>();
        services.AddSingleton<IStorageUsageService, StorageUsageService>();
        services.AddSingleton<IMacroTargetService, MacroTargetService>();
        services.AddSingleton<MacroCalculatorService>();
        services.AddIngredientParsing();
        services.AddSeasonality();
        services.AddSingleton<IGroceryListService, GroceryListService>();
        services.AddSingleton<IngredientCartService>();
        services.AddSingleton<IDailyFoodPlanService, DailyFoodPlanService>();
        services.AddSingleton<IMealPrepPlanService, MealPrepPlanService>();
        services.AddSingleton<IFoodFileService, FoodFileService>();
        services.AddSingleton<IUsdaFoodSearchService, NullUsdaFoodSearchService>();
        services.AddSingleton<IPantryService, PantryService>();
        services.AddSingleton<IImportFormat, PaprikaHtmlImportFormat>();
        services.AddSingleton<IImportFormat, BargainBoxPasteImportFormat>();
        services.AddSingleton<RecipeImportService>();
        services.AddSingleton<ImportDialogViewModel>();

        // Nav page view models: registered as singletons so switching between Recipes/Planning/
        // Groceries and back preserves each page's in-progress state (e.g. Recipes' list/detail/
        // edit sub-navigation) instead of resetting it every time.
        services.AddSingleton<RecipesListViewModel>();
        services.AddSingleton<MacroTargetsViewModel>();
        services.AddSingleton<DayPlanViewModel>();
        services.AddSingleton<MealPrepViewModel>();
        services.AddSingleton<PlanningPageViewModel>();
        services.AddSingleton<GroceriesViewModel>();
        services.AddSingleton<PantryViewModel>();

        // SettingsViewModel is registered normally, but MainViewModel only ever depends on the
        // Lazy<T> wrapper below, so it isn't constructed - and doesn't touch the file system to
        // read the stored Drive account - until the user actually opens the settings flyout.
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<FoodDatabaseViewModel>();
        services.AddSingleton<RecipeSettingsViewModel>();
        services.AddSingleton<SettingsPageViewModel>();
        services.AddSingleton(serviceProvider => new Lazy<SettingsViewModel>(() => serviceProvider.GetRequiredService<SettingsViewModel>()));

        // Non-initial nav pages are wrapped in Lazy<T> so MainViewModel doesn't construct them
        // (and their database/file work) until the user actually navigates to them, keeping the
        // window fast to show on startup.
        services.AddSingleton(serviceProvider => new Lazy<PlanningPageViewModel>(() => serviceProvider.GetRequiredService<PlanningPageViewModel>()));
        services.AddSingleton(serviceProvider => new Lazy<GroceriesViewModel>(() => serviceProvider.GetRequiredService<GroceriesViewModel>()));
        services.AddSingleton(serviceProvider => new Lazy<PantryViewModel>(() => serviceProvider.GetRequiredService<PantryViewModel>()));
        services.AddSingleton(serviceProvider => new Lazy<SettingsPageViewModel>(() => serviceProvider.GetRequiredService<SettingsPageViewModel>()));

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<StorageUsageFooterViewModel>();

        return services;
    }
}
