using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.Seasonality;

public static class SeasonalitySliceExtensions
{
    /// <summary>
    /// Registers all Seasonality slice services: ISeasonalityService.
    /// </summary>
    public static IServiceCollection AddSeasonalitySlice(this IServiceCollection services)
    {
        services.AddSingleton<ISeasonalityService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalJsonSeasonalityService>>();
            return new LocalJsonSeasonalityService(logger);
        });
        return services;
    }
}

