using System.ComponentModel;

namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// App-wide, observable view of Google Drive sync state. Owns the single source of truth for
/// "is a sync running", "when did we last sync", and "are there local changes not yet pushed",
/// so every trigger (startup, shutdown push, Settings "Sync now", footer "Sync now") is reflected
/// in one place — and any UI that binds to it updates for syncs it didn't itself start.
/// </summary>
public interface ISyncStatusService : INotifyPropertyChanged
{
    /// <summary>True while a sync/push is running anywhere in the app.</summary>
    bool IsSyncing { get; }

    /// <summary>True when a Google account is connected for backup.</summary>
    bool IsConnected { get; }

    /// <summary>Timestamp of the last fully-successful sync, or null if never synced.</summary>
    DateTime? LastSyncedAtUtc { get; }

    /// <summary>
    /// True when local data has changed since the last sync and Drive is connected, so a sync
    /// would actually push something. False while disconnected or nothing is dirty.
    /// </summary>
    bool HasUnsyncedChanges { get; }

    /// <summary>Error message from the most recent sync attempt, or null when all is well.</summary>
    string? LastSyncError { get; }

    /// <summary>Re-reads connection/last-synced/dirty state from the underlying services.</summary>
    void RefreshStatus();

    /// <summary>Full two-way sync, reporting via <paramref name="progress"/>. Re-entrancy-safe.</summary>
    Task<SyncResult> SyncNowAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Best-effort push-only pass (used at shutdown). Re-entrancy-safe.</summary>
    Task<SyncResult> PushOnlyAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Conflicts detected by a previous sync that still need the user to pick a side.</summary>
    IReadOnlyList<SyncConflict> GetPendingConflicts();

    /// <summary>Keeps the local version of a conflicted recipe/database and pushes it to Drive, clearing the conflict.</summary>
    Task ResolveConflictKeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken = default);

    /// <summary>Replaces the local version of a conflicted recipe/database with the Drive version, clearing the conflict.</summary>
    Task ResolveConflictUseDriveAsync(SyncConflict conflict, CancellationToken cancellationToken = default);
}
