﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Broccoli.App.Shared.Services;
using Broccoli.App.Shared.Configuration;
using Broccoli.App.Services;
using Broccoli.Shared.Services;
using Ginger.Data.Services;
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
        using var streamEnv = assembly.GetManifestResourceStream($"Broccoli.App.appsettings.{environment}.json");

        var configuration = new ConfigurationBuilder();
        
        if (streamBase != null)
        {
            configuration.AddJsonStream(streamBase);
        }
        
        if (streamEnv != null)
        {
            configuration.AddJsonStream(streamEnv);
        }
        
        var config = configuration.Build();
        
        // Register configuration
        builder.Configuration.AddConfiguration(config);
        
        // Get CosmosDB settings from configuration
        var cosmosSettings = new CosmosDbSettings();
        config.GetSection(CosmosDbSettings.SectionName).Bind(cosmosSettings);
        builder.Services.AddSingleton(cosmosSettings);
        
        // Configure CosmosDB Client with settings
        var cosmosClientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };
        
        // Add SSL bypass for emulator
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

        builder.Services.AddMauiBlazorWebView();
        
        // Register FoodService
        string foodDatabasePath = Path.Combine(FileSystem.AppDataDirectory, "..", "..", "..", "..", "..", "..", "Ginger.Data", "Data", "FoodDatabase.json");
        if (!File.Exists(foodDatabasePath))
        {
            // Fallback for different deployment scenarios
            foodDatabasePath = Path.Combine(AppContext.BaseDirectory, "FoodDatabase.json");
        }
        builder.Services.AddSingleton<IFoodService>(_ => new LocalJsonFoodService(foodDatabasePath));


#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        
        // Initialize CosmosDB on startup
        var cosmosDbService = app.Services.GetRequiredService<ICosmosDbService>();
        Task.Run(async () => await cosmosDbService.InitializeAsync());

        return app;
    }
}

