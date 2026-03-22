using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.IngredientParsing;

public static class IngredientParsingExtensions
{
    /// <summary>
    /// Registers the ingredient parsing pipeline:
    /// IFoodService → LocalJsonFoodService, IngredientParserService.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="foodDatabasePath">Absolute path to FoodDatabase.json.</param>
    public static IServiceCollection AddIngredientParsing(
        this IServiceCollection services,
        string foodDatabasePath)
    {
        services.AddSingleton<IFoodService>(sp =>
            new LocalJsonFoodService(
                foodDatabasePath,
                sp.GetRequiredService<ILogger<LocalJsonFoodService>>()));

        services.AddSingleton<IngredientParserService>();

        return services;
    }
}

