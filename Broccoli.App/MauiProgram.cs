using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Broccoli.App.Shared.Configuration;
using Broccoli.App.Shared.Infrastructure;
using Broccoli.App.Shared.IngredientParsing;
using Broccoli.App.Shared.Platform;
using Broccoli.App.Shared.Slices.Auth;
using Broccoli.App.Shared.Slices.AppSettings;
using Broccoli.App.Shared.Slices.Foods;
using Broccoli.App.Shared.Slices.GroceryList;
using Broccoli.App.Shared.Slices.MealPrep;
using Broccoli.App.Shared.Slices.Nutrition;
using Broccoli.App.Shared.Slices.Pantry;
using Broccoli.App.Shared.Slices.Recipes;
using Broccoli.App.Shared.Slices.Seasonality;
using Broccoli.App.Services;
using Microsoft.Azure.Cosmos;
namespace Broccoli.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });
        // Load configuration from embedded appsettings.json files
        var assembly = Assembly.GetExecutingAssembly();
#if DEBUG
        var environment = "Development";
#else
        var environment = "Production";
#endif
        using var streamBase = assembly.GetManifestResourceStream("Broccoli.App.appsettings.json");
        using var streamEnv  = assembly.GetManifestResourceStream($"Broccoli.App.appsettings.{environment}.json");
        var configuration = new ConfigurationBuilder();
        if (streamBase != null) configuration.AddJsonStream(streamBase);
        if (streamEnv  != null) configuration.AddJsonStream(streamEnv);
        var config = configuration.Build();
        builder.Configuration.AddConfiguration(config);
        // -- CosmosDB -------------------------------------------------------
        var cosmosSettings = new CosmosDbSettings();
        config.GetSection(CosmosDbSettings.SectionName).Bind(cosmosSettings);
        builder.Services.AddSingleton(cosmosSettings);
        var cosmosClientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };
        if (cosmosSettings.BypassSslValidation && cosmosSettings.IsEmulator())
        {
            cosmosClientOptions.HttpClientFactory = () =>
            {
                var h = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(h);
            };
        }
        builder.Services.AddSingleton(new CosmosClient(
            cosmosSettings.GetConnectionString(),
            cosmosClientOptions));
        // -- Cloudinary -----------------------------------------------------
        var cloudinarySettings = new CloudinarySettings();
        config.GetSection(CloudinarySettings.SectionName).Bind(cloudinarySettings);
        builder.Services.AddSingleton(cloudinarySettings);
        // -- Platform abstractions (host-specific) --------------------------
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        // -- FoodDatabase path ----------------------------------------------
        string foodDatabasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "..", "..", "..", "..", "..", "..",
            "Ginger.Data", "Data", "FoodDatabase.json");
        if (!File.Exists(foodDatabasePath))
            foodDatabasePath = Path.Combine(AppContext.BaseDirectory, "FoodDatabase.json");
        // -- Slice registrations --------------------------------------------
        builder.Services.AddAuthSlice();
        builder.Services.AddAppSettingsSlice();
        builder.Services.AddIngredientParsing(foodDatabasePath);
        builder.Services.AddSeasonalitySlice();
        builder.Services.AddPantrySlice();
        builder.Services.AddGroceryListSlice();
        // Food database editing is not supported on MAUI � flag permanently off
        builder.Services.AddFoodsSlice(new FeatureFlagsSettings { FoodDatabaseEditing = false });
        builder.Services.AddRecipesSlice();
        builder.Services.AddMealPrepSlice();
        builder.Services.AddNutritionSlice();
        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        // Initialize CosmosDB and all Cosmos-backed services on startup
        Task.Run(async () =>
        {
            var cosmosDbService = app.Services.GetRequiredService<ICosmosDbService>();
            await cosmosDbService.InitializeAsync();

            var pantryService = app.Services.GetRequiredService<IPantryService>();
            await pantryService.InitializeAsync();

            var groceryListService = app.Services.GetRequiredService<IGroceryListService>();
            await groceryListService.InitializeAsync();

            var macroTargetService = app.Services.GetRequiredService<IMacroTargetService>();
            await macroTargetService.InitializeAsync();

            var mealPrepPlanService = app.Services.GetRequiredService<IMealPrepPlanService>();
            await mealPrepPlanService.InitializeAsync();

            var dailyFoodPlanService = app.Services.GetRequiredService<IDailyFoodPlanService>();
            await dailyFoodPlanService.InitializeAsync();
        });
        return app;
    }
}