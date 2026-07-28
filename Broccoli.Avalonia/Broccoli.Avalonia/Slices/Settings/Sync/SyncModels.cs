namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// Tiny marker file stored at the root of the app's Drive folder recording the current
/// "generation" of synced data. Devices compare this against their own <see cref="SyncState.LastSyncedVersion"/>
/// to cheaply detect "has anything changed remotely since I last synced?" without downloading
/// the full database or every recipe.
/// </summary>
public class SyncManifest
{
    public int Version { get; set; }
    public string UpdatedByDeviceId { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>Local, per-device record of sync progress. Never uploaded to Drive.</summary>
public class SyncState
{
    /// <summary>Random id generated once per install, used only to label who made the last push (diagnostics).</summary>
    public string DeviceId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>The <see cref="SyncManifest.Version"/> this device last successfully synced to.</summary>
    public int LastSyncedVersion { get; set; }

    /// <summary>
    /// Wall-clock time of the last successful sync. Used as the dividing line for conflict
    /// detection: anything changed (locally or remotely) after this point is "new" and must be
    /// reconciled; anything before it is already-synced history.
    /// </summary>
    public DateTime? LastSyncedAtUtc { get; set; }

    /// <summary>Cached Drive folder id for the app's root "Broccoli" folder (avoids repeated lookups).</summary>
    public string? DriveRootFolderId { get; set; }

    /// <summary>Cached Drive folder id for the "Recipes" subfolder.</summary>
    public string? DriveRecipesFolderId { get; set; }
}

public enum SyncConflictKind
{
    Recipe,
    Database
}

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

/// <summary>Outcome of a <see cref="IGoogleDriveSyncService"/> sync/push run, shown in Settings.</summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<SyncConflict> Conflicts { get; set; } = new();
    public int RecipesPulled { get; set; }
    public int RecipesPushed { get; set; }
    public bool DatabasePulled { get; set; }
    public bool DatabasePushed { get; set; }

    public bool HasConflicts => Conflicts.Count > 0;

    public static SyncResult NotConnected { get; } = new() { Success = false, ErrorMessage = "Google Drive isn't connected." };
}
