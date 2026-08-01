using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.Pantry;

public static class PantrySliceExtensions
{
    /// <summary>
    /// Registers all Pantry slice services: IPantryService.
    /// </summary>
    public static IServiceCollection AddPantrySlice(this IServiceCollection services)
    {
        services.AddSingleton<IPantryService, PantryService>();
        return services;
    }
}

