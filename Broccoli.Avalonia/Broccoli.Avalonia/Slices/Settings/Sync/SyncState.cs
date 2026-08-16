namespace Broccoli.Avalonia.Slices.Settings.Sync;

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
