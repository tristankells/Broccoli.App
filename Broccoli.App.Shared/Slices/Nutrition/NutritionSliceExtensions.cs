using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.Nutrition;

public static class NutritionSliceExtensions
{
    /// <summary>
    /// Registers all Nutrition slice services:
    /// IMacroTargetService, IDailyFoodPlanService, MacroCalculatorService.
    /// Requires IngredientParserService to be registered (via AddIngredientParsing).
    /// </summary>
    public static IServiceCollection AddNutritionSlice(this IServiceCollection services)
    {
        services.AddSingleton<IMacroTargetService, CosmosMacroTargetService>();
        services.AddSingleton<IDailyFoodPlanService, CosmosDailyFoodPlanService>();
        services.AddSingleton<MacroCalculatorService>();
        return services;
    }
}

