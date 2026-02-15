using Broccoli.App.Web.Components;
using Broccoli.App.Shared.Services;
using Broccoli.App.Shared.Configuration;
using Broccoli.App.Web.Services;
using Broccoli.Shared.Services;
using Ginger.Data.Services;
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

// Add device-specific services used by the Broccoli.App.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();

// Add shared services
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();
builder.Services.AddSingleton<IRecipeService, CosmosRecipeService>();

// Register FoodService
// Try multiple paths for the FoodDatabase.json file
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
    if (File.Exists(fullPath))
    {
        foodDatabasePath = fullPath;
        break;
    }
}

if (string.IsNullOrEmpty(foodDatabasePath))
{
    // If no file found, use a default path (will fail gracefully in the service)
    foodDatabasePath = Path.Combine(builder.Environment.ContentRootPath, "FoodDatabase.json");
}

builder.Services.AddSingleton<IFoodService>(_ => new LocalJsonFoodService(foodDatabasePath));
builder.Services.AddSingleton<IFoodService>(_ => new LocalJsonFoodService(foodDatabasePath));

WebApplication app = builder.Build();

// Initialize CosmosDB
var cosmosDbService = app.Services.GetRequiredService<ICosmosDbService>();
await cosmosDbService.InitializeAsync();

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