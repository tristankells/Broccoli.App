using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.Avalonia.IngredientParsing;

public static class IngredientParsingExtensions
{
    public static IServiceCollection AddIngredientParsing(this IServiceCollection services)
    {
        services.AddSingleton<IFoodService, LocalJsonFoodService>();
        services.AddSingleton<IngredientParserService>();
        return services;
    }
}
