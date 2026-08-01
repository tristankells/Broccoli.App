using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.Foods;

public static class FoodsSliceExtensions
{
    /// <summary>
    /// Registers Foods slice services.
    /// <see cref="NullUsdaFoodSearchService"/> is registered as the default
    /// <see cref="IUsdaFoodSearchService"/>. Host projects that have a USDA API key
    /// configured should call AddHttpClient&lt;IUsdaFoodSearchService, UsdaFoodSearchService&gt;
    /// afterwards to override it with the real implementation.
    /// </summary>
    public static IServiceCollection AddFoodsSlice(this IServiceCollection services)
    {
        services.AddSingleton<IUsdaFoodSearchService, NullUsdaFoodSearchService>();
        return services;
    }
}
