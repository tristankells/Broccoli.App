using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// Orchestrates two-way sync of the local app-data folder with Google Drive for a single user
/// across multiple devices, under the assumption that devices are used sequentially (not
/// simultaneously) — see the design discussion this implements:
///
///   - Recipes (Markdown + images, already file-per-record) are compared and synced individually.
///   - The SQLite database is treated as one opaque unit (whole-file compare/replace) since its
///     contents can't be diffed; a clean snapshot is taken via `VACUUM INTO` before every upload.
///   - A tiny <see cref="SyncManifest"/> + local <see cref="SyncState"/> let devices cheaply detect
///     "has anything changed since I last synced?" without downloading the whole store every time.
///   - Deletions are tracked as tombstones (<see cref="TombstoneStore"/>) so removing a recipe on
///     one device correctly removes it everywhere instead of being silently re-uploaded by a
///     device that still has the old local copy.
///   - If the *same* recipe (or the database) changed on both sides since the last sync — the rare
///     "edited offline on two devices" case — nothing is auto-merged or silently overwritten: the
///     remote version is downloaded as a conflict copy and the user is asked to pick a side from
///     Settings (see <see cref="ResolveConflictKeepLocalAsync"/> / <see cref="ResolveConflictUseDriveAsync"/>).
/// </summary>
public interface IGoogleDriveSyncService
{
    /// <summary>Timestamp of the last fully-successful sync (from local state), or null if never synced.</summary>
    DateTime? LastSyncedAtUtc { get; }

    /// <summary>Conflicts detected by a previous sync that still need the user to pick a side.</summary>
    IReadOnlyList<SyncConflict> GetPendingConflicts();

    /// <summary>
    /// Full two-way sync: pulls remote changes, pushes local changes, detects conflicts.
    /// Safe to call on startup and from a manual "Sync now" button. Reports stage progress via
    /// <paramref name="progress"/>.
    /// </summary>
    Task<SyncResult> SyncAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort, push-only pass used when the app is closing: only pushes local changes, and
    /// only if Drive hasn't moved ahead since our last full sync (otherwise it backs off and
    /// leaves reconciliation to the next full <see cref="SyncAsync"/> on startup, to avoid ever
    /// risking a silent overwrite while the app has no time to show a conflict prompt).
    /// </summary>
    Task<SyncResult> PushOnlyAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Keeps the local version of a conflicted recipe/database and pushes it to Drive, clearing the conflict.</summary>
    Task ResolveConflictKeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken = default);

    /// <summary>Replaces the local version of a conflicted recipe/database with the Drive version, clearing the conflict.</summary>
    Task ResolveConflictUseDriveAsync(SyncConflict conflict, CancellationToken cancellationToken = default);
}
