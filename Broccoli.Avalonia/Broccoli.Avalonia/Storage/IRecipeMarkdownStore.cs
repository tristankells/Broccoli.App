using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Reads/writes <see cref="Recipe"/> as a human-readable Markdown file with a YAML
/// frontmatter block for structured fields, stored one-folder-per-recipe alongside its
/// images (see <see cref="AppPaths.RecipeFolder"/>). This keeps recipes easy to read/edit
/// outside the app and cheap to back up incrementally (only changed files re-sync).
/// </summary>
public interface IRecipeMarkdownStore
{
    /// <summary>Loads every recipe found under the Recipes folder.</summary>
    IReadOnlyList<Recipe> LoadAll();

    /// <summary>Loads a single recipe by id, or null if it doesn't exist.</summary>
    Recipe? Load(string recipeId);

    /// <summary>Writes (creates or overwrites) a recipe's Markdown file.</summary>
    void Save(Recipe recipe);

    /// <summary>Deletes a recipe's entire folder (Markdown file + images).</summary>
    void Delete(string recipeId);

    /// <summary>
    /// Copies an image file into the recipe's folder and returns the stored filename
    /// to append to <see cref="Recipe.Images"/>.
    /// </summary>
    string AddImage(string recipeId, string sourceFilePath);

    /// <summary>Deletes a previously-added image file from the recipe's folder.</summary>
    void RemoveImage(string recipeId, string fileName);
}
