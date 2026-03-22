using Broccoli.App.Shared.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.AppSettings;

public static class AppSettingsSliceExtensions
{
    /// <summary>
    /// Registers all AppSettings slice services: IThemeService.
    /// Note: ISecureStorageService (used by ThemeService) must be registered
    /// by the host project before calling this method.
    /// </summary>
    public static IServiceCollection AddAppSettingsSlice(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();
        return services;
    }
}

