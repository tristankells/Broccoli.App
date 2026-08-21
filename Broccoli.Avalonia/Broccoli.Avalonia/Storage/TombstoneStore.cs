using System.Text.Json;

namespace Broccoli.Avalonia.Storage;

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
        string path = AppPaths.TombstonesFilePath;
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
        TombstoneFile file = Load();
        RecipeTombstone? existing = file.Recipes.FirstOrDefault(r => r.RecipeId == recipeId);
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

    /// <summary>Records (or refreshes) a deletion for a single recipe-history snapshot.</summary>
    public static void RecordSnapshotDeletion(string recipeId, string snapshotId)
    {
        TombstoneFile file = Load();
        SnapshotTombstone? existing = file.Snapshots.FirstOrDefault(s => s.RecipeId == recipeId && s.SnapshotId == snapshotId);
        if (existing is not null)
        {
            existing.DeletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            file.Snapshots.Add(new SnapshotTombstone { RecipeId = recipeId, SnapshotId = snapshotId, DeletedAtUtc = DateTime.UtcNow });
        }

        Save(file);
    }

    /// <summary>
    /// Merges a remote tombstone list into the local one (union, keeping the latest
    /// <see cref="RecipeTombstone.DeletedAtUtc"/> per recipe id and the latest
    /// <see cref="SnapshotTombstone.DeletedAtUtc"/> per snapshot), and persists the merged result.
    /// </summary>
    public static TombstoneFile MergeWithRemote(TombstoneFile remote)
    {
        TombstoneFile local = Load();
        var mergedRecipes = local.Recipes.ToDictionary(r => r.RecipeId);

        foreach (RecipeTombstone remoteEntry in remote.Recipes)
        {
            if (!mergedRecipes.TryGetValue(remoteEntry.RecipeId, out RecipeTombstone? localEntry) ||
                remoteEntry.DeletedAtUtc > localEntry.DeletedAtUtc)
            {
                mergedRecipes[remoteEntry.RecipeId] = remoteEntry;
            }
        }

        var mergedSnapshots = local.Snapshots.ToDictionary(s => (s.RecipeId, s.SnapshotId));

        foreach (SnapshotTombstone remoteEntry in remote.Snapshots)
        {
            (string RecipeId, string SnapshotId) key = (remoteEntry.RecipeId, remoteEntry.SnapshotId);
            if (!mergedSnapshots.TryGetValue(key, out SnapshotTombstone? localEntry) ||
                remoteEntry.DeletedAtUtc > localEntry.DeletedAtUtc)
            {
                mergedSnapshots[key] = remoteEntry;
            }
        }

        var mergedFile = new TombstoneFile
        {
            Recipes = mergedRecipes.Values.ToList(),
            Snapshots = mergedSnapshots.Values.ToList(),
        };
        Save(mergedFile);
        return mergedFile;
    }
}
