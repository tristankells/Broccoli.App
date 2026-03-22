using Broccoli.App.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Broccoli.App.Shared.Slices.Recipes;

public static class RecipesSliceExtensions
{
    /// <summary>
    /// Registers all Recipes slice services:
    /// IRecipeService, IRecipeImageService, IImportFormat (Paprika), RecipeImportService.
    /// Requires CloudinarySettings to be registered before calling this method.
    /// </summary>
    public static IServiceCollection AddRecipesSlice(this IServiceCollection services)
    {
        services.AddSingleton<IRecipeService, CosmosRecipeService>();
        services.AddSingleton<IRecipeImageService, CloudinaryImageService>();
        services.AddSingleton<Import.IImportFormat, Import.PaprikaHtmlImportFormat>();
        services.AddSingleton<Import.RecipeImportService>();
        return services;
    }
}

