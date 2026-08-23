using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.Avalonia.IngredientParsing;

public static class IngredientParsingExtensions
{
    public static IServiceCollection AddIngredientParsing(this IServiceCollection services)
    {
        services.AddSingleton<IFoodService, FoodService>();
        services.AddSingleton<IngredientParserService>();
        return services;
    }
}
