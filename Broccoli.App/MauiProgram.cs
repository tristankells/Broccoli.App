﻿using Microsoft.Extensions.Logging;
using Broccoli.App.Shared.Services;
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

        // CosmosDB Configuration
        var cosmosEndpoint = "https://localhost:8081";
        var cosmosKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        
        var cosmosClientOptions = new CosmosClientOptions
        {
            HttpClientFactory = () =>
            {
                var httpMessageHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(httpMessageHandler);
            },
            ConnectionMode = ConnectionMode.Gateway
        };

        builder.Services.AddSingleton(new CosmosClient(cosmosEndpoint, cosmosKey, cosmosClientOptions));

        // Add device-specific services used by the Broccoli.App.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        
        // Add shared services
        builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();

        builder.Services.AddMauiBlazorWebView();

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