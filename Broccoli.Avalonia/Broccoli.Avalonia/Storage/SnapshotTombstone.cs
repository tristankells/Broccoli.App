namespace Broccoli.Avalonia.Storage;

/// <summary>A single deleted recipe-history snapshot, so deletions propagate between devices instead of being silently resurrected.</summary>
public class SnapshotTombstone
{
    public string RecipeId { get; set; } = string.Empty;

    public string SnapshotId { get; set; } = string.Empty;

    public DateTime DeletedAtUtc { get; set; }
}
