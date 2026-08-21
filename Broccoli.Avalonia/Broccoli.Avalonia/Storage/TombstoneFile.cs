namespace Broccoli.Avalonia.Storage;

/// <summary>The full tombstone list, persisted locally and synced as one small JSON file alongside the manifest.</summary>
public class TombstoneFile
{
    public List<RecipeTombstone> Recipes { get; set; } = new();

    public List<SnapshotTombstone> Snapshots { get; set; } = new();
}
