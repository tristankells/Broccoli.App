using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Slices.Recipes;

public class RecipeService : IRecipeService
{
    private readonly IRecipeMarkdownStore _store;
    private readonly IRecipeHistoryStore _historyStore;
    private readonly IMacroTargetService _macroService;

    public RecipeService()
        : this(new RecipeMarkdownStore(), new RecipeHistoryStore(), new MacroTargetService())
    {
    }

    public RecipeService(IRecipeMarkdownStore store)
        : this(store, new RecipeHistoryStore(), new MacroTargetService())
    {
    }

    public RecipeService(IRecipeMarkdownStore store, IRecipeHistoryStore historyStore, IMacroTargetService macroService)
    {
        _store = store;
        _historyStore = historyStore;
        _macroService = macroService;
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
        Recipe? existing = _store.Load(recipe.Id);
        if (existing is not null && ContentChanged(existing, recipe))
        {
            int maxBackups = _macroService.GetSettings().RecipeHistoryBackupCount;
            _historyStore.Save(RecipeSnapshot.FromRecipe(existing, DateTime.UtcNow), maxBackups);
        }

        recipe.UpdatedAt = DateTime.UtcNow;
        _store.Save(recipe);
        return recipe;
    }

    public void Delete(string recipeId)
    {
        _historyStore.DeleteAll(recipeId);
        _store.Delete(recipeId);
    }

    public IReadOnlyList<RecipeSnapshot> GetHistory(string recipeId) => _historyStore.List(recipeId);

    public RecipeSnapshot? GetSnapshot(string recipeId, string snapshotId) => _historyStore.Get(recipeId, snapshotId);

    public Recipe? Restore(string recipeId, string snapshotId)
    {
        RecipeSnapshot? snapshot = _historyStore.Get(recipeId, snapshotId);
        Recipe? current = _store.Load(recipeId);
        if (snapshot is null || current is null)
        {
            return null;
        }

        current.Name = snapshot.Name;
        current.Ingredients = snapshot.Ingredients;
        current.Directions = snapshot.Directions;
        current.Notes = snapshot.Notes;
        current.Servings = snapshot.Servings;
        current.PrepTimeMinutes = snapshot.PrepTimeMinutes;
        current.CookTimeMinutes = snapshot.CookTimeMinutes;
        current.Source = snapshot.Source;
        current.Url = snapshot.Url;
        current.Tags = new List<string>(snapshot.Tags);

        return Update(current);
    }

    public Recipe AddImage(Recipe recipe, string sourceFilePath)
    {
        string fileName = _store.AddImage(recipe.Id, sourceFilePath);
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

    private static bool ContentChanged(Recipe existing, Recipe updated) =>
        !string.Equals(existing.Name, updated.Name, StringComparison.Ordinal) ||
        !string.Equals(Normalize(existing.Ingredients), Normalize(updated.Ingredients), StringComparison.Ordinal) ||
        !string.Equals(Normalize(existing.Directions), Normalize(updated.Directions), StringComparison.Ordinal) ||
        !string.Equals(Normalize(existing.Notes), Normalize(updated.Notes), StringComparison.Ordinal) ||
        existing.Servings != updated.Servings ||
        existing.PrepTimeMinutes != updated.PrepTimeMinutes ||
        existing.CookTimeMinutes != updated.CookTimeMinutes ||
        !string.Equals(Normalize(existing.Source), Normalize(updated.Source), StringComparison.Ordinal) ||
        !string.Equals(Normalize(existing.Url), Normalize(updated.Url), StringComparison.Ordinal) ||
        !(existing.Tags ?? new List<string>()).SequenceEqual(updated.Tags ?? new List<string>());

    private static string Normalize(string? value) => (value ?? string.Empty).Replace("\r\n", "\n").Trim();
}
