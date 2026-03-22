using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.GroceryList;

public static class GroceryListSliceExtensions
{
    /// <summary>
    /// Registers all GroceryList slice services: IGroceryListService, IngredientCartService.
    /// Requires IngredientParserService to be registered first (via AddIngredientParsing).
    /// </summary>
    public static IServiceCollection AddGroceryListSlice(this IServiceCollection services)
    {
        services.AddSingleton<IGroceryListService, GroceryListService>();
        services.AddSingleton<IngredientCartService>();
        return services;
    }
}

