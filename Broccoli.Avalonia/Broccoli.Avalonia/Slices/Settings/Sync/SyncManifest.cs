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
