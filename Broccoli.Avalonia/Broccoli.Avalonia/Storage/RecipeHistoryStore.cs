using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Stores <see cref="RecipeSnapshot"/>s as Markdown files under each recipe's <c>history/</c>
/// sub-folder. The first snapshot is treated as the "original" and is never pruned; retention
/// keeps that first version plus the <c>maxBackups</c>-1 most recent ones.
/// </summary>
public class RecipeHistoryStore : IRecipeHistoryStore
{
    private const string SnapshotFileExtension = "*.md";

    private readonly string _recipesFolder;

    public RecipeHistoryStore()
        : this(AppPaths.RecipesFolder)
    {
    }

    /// <summary>
    /// Initializes the store against a specific recipes folder (used by tests to isolate storage).
    /// </summary>
    public RecipeHistoryStore(string recipesFolder)
    {
        _recipesFolder = recipesFolder;
    }

    public IReadOnlyList<RecipeSnapshot> List(string recipeId) =>
        LoadEntries(recipeId)
            .Select(entry => entry.Snapshot)
            .OrderByDescending(snapshot => snapshot.CapturedAtUtc)
            .ThenByDescending(snapshot => snapshot.Id)
            .ToList();

    public RecipeSnapshot? Get(string recipeId, string snapshotId) =>
        LoadEntries(recipeId)
            .Select(entry => entry.Snapshot)
            .FirstOrDefault(snapshot => snapshot.Id == snapshotId);

    public void Save(RecipeSnapshot snapshot, int maxBackups)
    {
        string folder = HistoryFolder(snapshot.RecipeId);
        Directory.CreateDirectory(folder);

        string fileName = $"{snapshot.CapturedAtUtc:yyyyMMddHHmmssfff}-{snapshot.Id}.md";
        string content = RecipeMarkdownStore.Serialize(ToRecipe(snapshot));
        File.WriteAllText(Path.Combine(folder, fileName), content);

        Prune(snapshot.RecipeId, maxBackups);
    }

    public void DeleteAll(string recipeId)
    {
        string folder = HistoryFolder(recipeId);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private string HistoryFolder(string recipeId) =>
        Path.Combine(_recipesFolder, recipeId, AppPaths.RecipeHistoryFolderName);

    private static Recipe ToRecipe(RecipeSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Name = snapshot.Name,
        Ingredients = snapshot.Ingredients,
        Directions = snapshot.Directions,
        Notes = snapshot.Notes,
        Servings = snapshot.Servings,
        PrepTimeMinutes = snapshot.PrepTimeMinutes,
        CookTimeMinutes = snapshot.CookTimeMinutes,
        Source = snapshot.Source,
        Url = snapshot.Url,
        Tags = new List<string>(snapshot.Tags),
        Images = new List<string>(),
        CreatedAt = snapshot.CapturedAtUtc,
        UpdatedAt = null,
    };

    private static RecipeSnapshot ToSnapshot(Recipe recipe, string recipeId) => new()
    {
        Id = recipe.Id,
        RecipeId = recipeId,
        CapturedAtUtc = recipe.CreatedAt,
        Name = recipe.Name,
        Ingredients = recipe.Ingredients,
        Directions = recipe.Directions,
        Notes = recipe.Notes,
        Servings = recipe.Servings,
        PrepTimeMinutes = recipe.PrepTimeMinutes,
        CookTimeMinutes = recipe.CookTimeMinutes,
        Source = recipe.Source,
        Url = recipe.Url,
        Tags = new List<string>(recipe.Tags),
    };

    private List<(RecipeSnapshot Snapshot, string FilePath)> LoadEntries(string recipeId)
    {
        var entries = new List<(RecipeSnapshot Snapshot, string FilePath)>();
        string folder = HistoryFolder(recipeId);
        if (!Directory.Exists(folder))
        {
            return entries;
        }

        foreach (string filePath in Directory.EnumerateFiles(folder, SnapshotFileExtension))
        {
            Recipe recipe = RecipeMarkdownStore.Deserialize(
                File.ReadAllText(filePath),
                Path.GetFileNameWithoutExtension(filePath));
            entries.Add((ToSnapshot(recipe, recipeId), filePath));
        }

        return entries;
    }

    private void Prune(string recipeId, int maxBackups)
    {
        int keep = Math.Max(1, maxBackups);
        List<(RecipeSnapshot Snapshot, string FilePath)> entries = LoadEntries(recipeId)
            .OrderBy(entry => entry.Snapshot.CapturedAtUtc)
            .ThenBy(entry => entry.Snapshot.Id)
            .ToList();

        if (entries.Count <= keep)
        {
            return;
        }

        // Keep index 0 (the original) and the last (keep - 1) most recent; delete the middle.
        int removeCount = entries.Count - keep;
        for (int i = 1; i <= removeCount; i++)
        {
            File.Delete(entries[i].FilePath);
        }
    }
}
