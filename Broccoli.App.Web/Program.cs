using Broccoli.App.Web.Components;
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
using Broccoli.App.Web.Services;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Azure.Cosmos;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Enable static web assets for all environments (including Production during development)
// This ensures CSS and other static files from referenced projects load correctly
if (!builder.Environment.IsDevelopment())
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Get CosmosDB settings from configuration
var cosmosSettings = new CosmosDbSettings();
builder.Configuration.GetSection(CosmosDbSettings.SectionName).Bind(cosmosSettings);
builder.Services.AddSingleton(cosmosSettings);

// Log which environment and CosmosDB we're using
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
logger.LogInformation("===================================");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("CosmosDB Endpoint: {Endpoint}", cosmosSettings.EndpointUri);
logger.LogInformation("Is Emulator: {IsEmulator}", cosmosSettings.IsEmulator());
logger.LogInformation("SSL Bypass: {BypassSsl}", cosmosSettings.BypassSslValidation);
logger.LogInformation("===================================");

// Configure CosmosDB Client with settings
var cosmosClientOptions = new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
};

// Add SSL bypass for emulator only
if (cosmosSettings.BypassSslValidation && cosmosSettings.IsEmulator())
{
    cosmosClientOptions.HttpClientFactory = () =>
    {
        var httpMessageHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        return new HttpClient(httpMessageHandler);
    };
}

builder.Services.AddSingleton(new CosmosClient(
    cosmosSettings.GetConnectionString(),
    cosmosClientOptions));

// ── Platform abstractions (host-specific) ────────────────────────────────
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();

// ── Cloudinary (shared image storage settings) ────────────────────────────
var cloudinarySettings = new CloudinarySettings();
builder.Configuration.GetSection(CloudinarySettings.SectionName).Bind(cloudinarySettings);
builder.Services.AddSingleton(cloudinarySettings);

// ── Resolve FoodDatabase.json path ───────────────────────────────────────
string foodDatabasePath = string.Empty;
var possiblePaths = new[]
{
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "Ginger.Data", "Data", "FoodDatabase.json"),
    Path.Combine(builder.Environment.ContentRootPath, "FoodDatabase.json"),
    Path.Combine(AppContext.BaseDirectory, "FoodDatabase.json"),
    Path.Combine(AppContext.BaseDirectory, "Data", "FoodDatabase.json")
};
foreach (var path in possiblePaths)
{
    var fullPath = Path.GetFullPath(path);
    if (File.Exists(fullPath)) { foodDatabasePath = fullPath; break; }
}
if (string.IsNullOrEmpty(foodDatabasePath))
    foodDatabasePath = Path.Combine(builder.Environment.ContentRootPath, "FoodDatabase.json");

// ── Feature flags ─────────────────────────────────────────────────────────
var featureFlags = new FeatureFlagsSettings();
builder.Configuration.GetSection(FeatureFlagsSettings.SectionName).Bind(featureFlags);

// ── Dev credentials (auto-login in Development) ───────────────────────────
builder.Services.Configure<DevCredentialsSettings>(
    builder.Configuration.GetSection(DevCredentialsSettings.SectionName));

var usdaSettings = new UsdaSettings();
builder.Configuration.GetSection(UsdaSettings.SectionName).Bind(usdaSettings);

// ── Slice registrations ───────────────────────────────────────────────────
builder.Services.AddAuthSlice();
builder.Services.AddAppSettingsSlice();
builder.Services.AddIngredientParsing(foodDatabasePath);
builder.Services.AddSeasonalitySlice();
builder.Services.AddPantrySlice();
builder.Services.AddGroceryListSlice();
builder.Services.AddFoodsSlice(featureFlags);
if (featureFlags.FoodDatabaseEditing)
{
    builder.Services.AddSingleton(usdaSettings);
    builder.Services.AddHttpClient<IUsdaFoodSearchService, UsdaFoodSearchService>(client =>
        client.BaseAddress = new Uri(usdaSettings.BaseUrl.TrimEnd('/') + "/"));
}
builder.Services.AddRecipesSlice();
builder.Services.AddMealPrepSlice();
builder.Services.AddNutritionSlice();

WebApplication app = builder.Build();

// Initialize CosmosDB
var cosmosDbService = app.Services.GetRequiredService<ICosmosDbService>();
await cosmosDbService.InitializeAsync();

// Initialize Pantry and GroceryList containers
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(Broccoli.App.Shared._Imports).Assembly);

app.Run();