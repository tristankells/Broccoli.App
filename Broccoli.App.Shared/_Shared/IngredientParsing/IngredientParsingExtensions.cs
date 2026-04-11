using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.IngredientParsing;

public static class IngredientParsingExtensions
{
    /// <summary>
    /// Registers the ingredient parsing pipeline:
    /// IFoodService → CosmosFoodService (seeded from JSON on first launch), IngredientParserService.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="seedFilePath">
    /// Absolute path to FoodDatabase.json used to seed an empty container on first launch.
    /// Pass null or an empty string to skip seeding.
    /// </param>
    public static IServiceCollection AddIngredientParsing(
        this IServiceCollection services,
        string? seedFilePath)
    {
        services.AddSingleton<IFoodService>(sp =>
            new CosmosFoodService(
                sp.GetRequiredService<CosmosClient>(),
                seedFilePath,
                sp.GetRequiredService<ILogger<CosmosFoodService>>()));

        services.AddSingleton<IngredientParserService>();

        return services;
    }
}

