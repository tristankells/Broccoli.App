using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// Business-facing API for managing recipes, used by the Recipes UI. Wraps
/// <see cref="Storage.IRecipeMarkdownStore"/> and is responsible for id generation and
/// created/updated timestamps, so view models never need to touch storage details directly.
/// </summary>
public interface IRecipeService
{
    /// <summary>Returns all recipes, ordered by name.</summary>
    IReadOnlyList<Recipe> GetAll();

    /// <summary>Returns a single recipe by id, or null if it doesn't exist.</summary>
    Recipe? Get(string recipeId);

    /// <summary>Creates a new recipe (assigns a new id and CreatedAt) and persists it.</summary>
    Recipe Create(Recipe recipe);

    /// <summary>Persists changes to an existing recipe, updating UpdatedAt.</summary>
    Recipe Update(Recipe recipe);

    /// <summary>Permanently deletes a recipe and all its images.</summary>
    void Delete(string recipeId);

    /// <summary>Returns a recipe's ingredient-history snapshots, newest first.</summary>
    IReadOnlyList<RecipeSnapshot> GetHistory(string recipeId);

    /// <summary>Returns a single history snapshot by id, or null if it doesn't exist.</summary>
    RecipeSnapshot? GetSnapshot(string recipeId, string snapshotId);

    /// <summary>
    /// Replaces a recipe's current content with an earlier snapshot, preserving the current
    /// version as a snapshot first so the restore can be undone. Returns the restored recipe,
    /// or null if the recipe or snapshot no longer exists.
    /// </summary>
    Recipe? Restore(string recipeId, string snapshotId);

    /// <summary>
    /// Copies an image into the recipe's folder and appends it to <see cref="Recipe.Images"/>,
    /// returning the updated recipe.
    /// </summary>
    Recipe AddImage(Recipe recipe, string sourceFilePath);

    /// <summary>
    /// Removes an image from the recipe's folder and <see cref="Recipe.Images"/>, returning the
    /// updated recipe.
    /// </summary>
    Recipe RemoveImage(Recipe recipe, string fileName);

    /// <summary>Full path to a recipe's stored image, for display in the UI.</summary>
    string GetImagePath(string recipeId, string fileName);
}
