namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// A detected conflict: the same entity (a recipe, or the whole database) was changed on both
/// this device and Drive since the last successful sync. Nothing is auto-resolved — the local
/// version is left untouched and the remote version is saved alongside as a "conflict copy" so
/// no data is ever silently lost; the user picks a side from Settings.
/// </summary>
public class SyncConflict
{
    public SyncConflictKind Kind { get; init; }

    /// <summary>Recipe id (only set when Kind == Recipe).</summary>
    public string? RecipeId { get; init; }

    /// <summary>Display name for the conflict list (recipe name, or "Database").</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Path to the downloaded remote copy kept for comparison/recovery.</summary>
    public string ConflictCopyPath { get; init; } = string.Empty;

    public DateTime DetectedAtUtc { get; init; } = DateTime.UtcNow;
}
