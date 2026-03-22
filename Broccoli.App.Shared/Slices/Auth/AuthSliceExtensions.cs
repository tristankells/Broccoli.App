using Broccoli.App.Shared.Infrastructure;
using Broccoli.App.Shared.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.Auth;

public static class AuthSliceExtensions
{
    /// <summary>
    /// Registers all Auth slice services:
    /// ICosmosDbService, IAuthenticationService, IAuthenticationStateService.
    /// Note: IFormFactor and ISecureStorageService are host-specific and must be
    /// registered separately by each host project before calling this method.
    /// </summary>
    public static IServiceCollection AddAuthSlice(this IServiceCollection services)
    {
        services.AddSingleton<ICosmosDbService, CosmosDbService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();
        return services;
    }
}

