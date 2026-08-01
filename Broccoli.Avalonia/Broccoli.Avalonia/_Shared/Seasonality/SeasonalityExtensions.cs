using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.Avalonia.Seasonality;

public static class SeasonalityExtensions
{
    public static IServiceCollection AddSeasonality(this IServiceCollection services)
    {
        services.AddSingleton<ISeasonalityService, LocalJsonSeasonalityService>();
        return services;
    }
}
