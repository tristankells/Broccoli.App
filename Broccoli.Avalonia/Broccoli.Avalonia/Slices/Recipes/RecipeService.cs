using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Slices.Recipes;

public class RecipeService : IRecipeService
{
    private readonly IRecipeMarkdownStore _store;

    public RecipeService() : this(new RecipeMarkdownStore())
    {
    }

    public RecipeService(IRecipeMarkdownStore store)
    {
        _store = store;
    }

    public IReadOnlyList<Recipe> GetAll() =>
        _store.LoadAll().OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public Recipe? Get(string recipeId) => _store.Load(recipeId);

    public Recipe Create(Recipe recipe)
    {
        recipe.Id = string.IsNullOrWhiteSpace(recipe.Id) ? Guid.NewGuid().ToString() : recipe.Id;
        recipe.CreatedAt = DateTime.UtcNow;
        recipe.UpdatedAt = null;
        _store.Save(recipe);
        return recipe;
    }

    public Recipe Update(Recipe recipe)
    {
        recipe.UpdatedAt = DateTime.UtcNow;
        _store.Save(recipe);
        return recipe;
    }

    public void Delete(string recipeId) => _store.Delete(recipeId);

    public Recipe AddImage(Recipe recipe, string sourceFilePath)
    {
        var fileName = _store.AddImage(recipe.Id, sourceFilePath);
        if (!recipe.Images.Contains(fileName))
        {
            recipe.Images.Add(fileName);
        }

        return Update(recipe);
    }

    public Recipe RemoveImage(Recipe recipe, string fileName)
    {
        _store.RemoveImage(recipe.Id, fileName);
        recipe.Images.Remove(fileName);
        return Update(recipe);
    }

    public string GetImagePath(string recipeId, string fileName) =>
        Path.Combine(AppPaths.RecipeFolder(recipeId), fileName);
}
