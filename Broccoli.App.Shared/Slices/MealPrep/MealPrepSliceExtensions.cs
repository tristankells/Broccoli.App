using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.MealPrep;

public static class MealPrepSliceExtensions
{
    /// <summary>
    /// Registers all MealPrep slice services: IMealPrepPlanService.
    /// </summary>
    public static IServiceCollection AddMealPrepSlice(this IServiceCollection services)
    {
        services.AddSingleton<IMealPrepPlanService, CosmosMealPrepPlanService>();
        return services;
    }
}

