using System.Text.Json;

namespace Broccoli.Avalonia.Storage;

/// <summary>A single deleted recipe, so deletions propagate between devices instead of being silently resurrected.</summary>
public class RecipeTombstone
{
    public string RecipeId { get; set; } = string.Empty;
    public DateTime DeletedAtUtc { get; set; }
}

/// <summary>The full tombstone list, persisted locally and synced as one small JSON file alongside the manifest.</summary>
public class TombstoneFile
{
    public List<RecipeTombstone> Recipes { get; set; } = new();
}

/// <summary>
/// Reads/writes the local tombstone list (<see cref="AppPaths.TombstonesFilePath"/>).
/// <see cref="RecipeMarkdownStore.Delete"/> records a tombstone here automatically so the
/// Google Drive sync service knows to propagate the deletion to other devices instead of
/// treating it as "this device is missing a recipe that should be re-downloaded".
/// </summary>
public static class TombstoneStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static TombstoneFile Load()
    {
        var path = AppPaths.TombstonesFilePath;
        if (!File.Exists(path))
        {
            return new TombstoneFile();
        }

        try
        {
            return JsonSerializer.Deserialize<TombstoneFile>(File.ReadAllText(path)) ?? new TombstoneFile();
        }
        catch (JsonException)
        {
            return new TombstoneFile();
        }
    }

    public static void Save(TombstoneFile file) =>
        File.WriteAllText(AppPaths.TombstonesFilePath, JsonSerializer.Serialize(file, JsonOptions));

    /// <summary>Records (or refreshes) a deletion for <paramref name="recipeId"/>.</summary>
    public static void RecordDeletion(string recipeId)
    {
        var file = Load();
        var existing = file.Recipes.FirstOrDefault(r => r.RecipeId == recipeId);
        if (existing is not null)
        {
            existing.DeletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            file.Recipes.Add(new RecipeTombstone { RecipeId = recipeId, DeletedAtUtc = DateTime.UtcNow });
        }

        Save(file);
    }

    /// <summary>
    /// Merges a remote tombstone list into the local one (union, keeping the latest
    /// <see cref="RecipeTombstone.DeletedAtUtc"/> per recipe id), and persists the merged result.
    /// </summary>
    public static TombstoneFile MergeWithRemote(TombstoneFile remote)
    {
        var local = Load();
        var merged = local.Recipes.ToDictionary(r => r.RecipeId);

        foreach (var remoteEntry in remote.Recipes)
        {
            if (!merged.TryGetValue(remoteEntry.RecipeId, out var localEntry) ||
                remoteEntry.DeletedAtUtc > localEntry.DeletedAtUtc)
            {
                merged[remoteEntry.RecipeId] = remoteEntry;
            }
        }

        var mergedFile = new TombstoneFile { Recipes = merged.Values.ToList() };
        Save(mergedFile);
        return mergedFile;
    }
}
