namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// Measures how much storage the app is taking up, broken down by the kinds of data it syncs to
/// Google Drive: Markdown recipes, their image files, recipe-history "backup" snapshots, and the
/// SQLite database. Local file sizes are a faithful proxy for the app's Drive footprint because
/// sync mirrors the local layout 1:1.
/// </summary>
public interface IStorageUsageService
{
    /// <summary>
    /// Walks the local app-data folder and buckets every file into one of the four categories.
    /// Synchronous and offline — never touches the network.
    /// </summary>
    StorageUsageSnapshot ComputeLocalUsage();

    /// <summary>
    /// Returns the connected Google account's overall Drive quota (used / limit) from
    /// <c>About.Get</c>, or null if Drive isn't connected or the call fails. Best-effort.
    /// </summary>
    Task<DriveQuota?> GetDriveQuotaAsync(CancellationToken cancellationToken = default);
}
