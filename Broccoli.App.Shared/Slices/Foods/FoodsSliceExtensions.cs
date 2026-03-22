using Broccoli.App.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.Foods;

public static class FoodsSliceExtensions
{
    /// <summary>
    /// Registers Foods slice configuration.
    /// FeatureFlagsSettings is registered here so Foods.razor can inject it.
    /// IUsdaFoodSearchService must be registered by the host when FoodDatabaseEditing is true
    /// because AddHttpClient is only available in host projects.
    /// MAUI always passes FoodDatabaseEditing = false; no USDA service is needed.
    /// </summary>
    public static IServiceCollection AddFoodsSlice(
        this IServiceCollection services,
        FeatureFlagsSettings featureFlags)
    {
        services.AddSingleton(featureFlags);
        return services;
    }
}
