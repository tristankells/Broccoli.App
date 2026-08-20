using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Reads/writes a recipe's ingredient-history snapshots, one Markdown file per captured version
/// under the recipe's <c>history/</c> sub-folder (see <see cref="AppPaths.RecipeHistoryFolder"/>).
/// Snapshots reuse the same Markdown + YAML format as the recipe itself, so they remain human-readable.
/// </summary>
public interface IRecipeHistoryStore
{
    /// <summary>Returns all snapshots for a recipe, newest first.</summary>
    IReadOnlyList<RecipeSnapshot> List(string recipeId);

    /// <summary>Returns a single snapshot by id, or null if it doesn't exist.</summary>
    RecipeSnapshot? Get(string recipeId, string snapshotId);

    /// <summary>
    /// Writes a snapshot and then prunes old ones so that at most <paramref name="maxBackups"/>
    /// remain, always keeping the oldest (first) snapshot.
    /// </summary>
    void Save(RecipeSnapshot snapshot, int maxBackups);

    /// <summary>Deletes all snapshots for a recipe.</summary>
    void DeleteAll(string recipeId);

    /// <summary>Deletes a single snapshot for a recipe, if it exists.</summary>
    void Delete(string recipeId, string snapshotId);
}
