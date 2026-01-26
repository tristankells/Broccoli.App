using Broccoli.App.Web.Components;
using Broccoli.App.Shared.Services;
using Broccoli.App.Web.Services;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
builder.Services.AddScoped<ISecureStorageService, SecureStorageService>();

// Add shared services
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IAuthenticationStateService, AuthenticationStateService>();

var app = builder.Build();

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