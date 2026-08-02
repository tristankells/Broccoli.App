using System.Text;
using System.Text.Json;
using Broccoli.Avalonia.Storage;
using Google.Apis.Drive.v3;
using Microsoft.Data.Sqlite;

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
    /// Safe to call on startup and from a manual "Sync now" button.
    /// </summary>
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort, push-only pass used when the app is closing: only pushes local changes, and
    /// only if Drive hasn't moved ahead since our last full sync (otherwise it backs off and
    /// leaves reconciliation to the next full <see cref="SyncAsync"/> on startup, to avoid ever
    /// risking a silent overwrite while the app has no time to show a conflict prompt).
    /// </summary>
    Task<SyncResult> PushOnlyAsync(CancellationToken cancellationToken = default);

    /// <summary>Keeps the local version of a conflicted recipe/database and pushes it to Drive, clearing the conflict.</summary>
    Task ResolveConflictKeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken = default);

    /// <summary>Replaces the local version of a conflicted recipe/database with the Drive version, clearing the conflict.</summary>
    Task ResolveConflictUseDriveAsync(SyncConflict conflict, CancellationToken cancellationToken = default);
}

public class GoogleDriveSyncService : IGoogleDriveSyncService
{
    private const string ManifestFileName = "sync-manifest.json";
    private const string TombstonesFileName = "tombstones.json";
    private const string DatabaseFileName = "broccoli.db";
    private const string RecipesFolderName = "Recipes";
    private const string RecipeMarkdownFileName = "recipe.md";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IGoogleDriveAuthService _authService;

    public GoogleDriveSyncService(IGoogleDriveAuthService authService)
    {
        _authService = authService;
    }

    public DateTime? LastSyncedAtUtc => LoadState().LastSyncedAtUtc;

    public IReadOnlyList<SyncConflict> GetPendingConflicts() => LoadConflicts();

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var drive = await _authService.TryGetDriveServiceAsync(cancellationToken);
        if (drive is null)
        {
            return SyncResult.NotConnected;
        }

        var result = new SyncResult { Success = true };
        var state = LoadState();
        var sinceUtc = state.LastSyncedAtUtc ?? DateTime.MinValue;

        try
        {
            var rootFolderId = await GetOrCreateFolderIdAsync(drive, state, isRecipesFolder: false, cancellationToken);
            var recipesFolderId = await GetOrCreateFolderIdAsync(drive, state, isRecipesFolder: true, cancellationToken);

            var manifest = await ReadJsonFromDriveAsync<SyncManifest>(drive, rootFolderId, ManifestFileName, cancellationToken)
                           ?? new SyncManifest { Version = 0 };

            var remoteTombstones = await ReadJsonFromDriveAsync<TombstoneFile>(drive, rootFolderId, TombstonesFileName, cancellationToken)
                                   ?? new TombstoneFile();
            var mergedTombstones = TombstoneStore.MergeWithRemote(remoteTombstones);

            var conflicts = LoadConflicts();
            var didPushAnything = false;

            didPushAnything |= await SyncTombstonesAsync(drive, recipesFolderId, mergedTombstones, cancellationToken);
            didPushAnything |= await SyncRecipesAsync(drive, recipesFolderId, mergedTombstones, sinceUtc, conflicts, result, cancellationToken);
            didPushAnything |= await SyncDatabaseAsync(drive, rootFolderId, sinceUtc, conflicts, result, cancellationToken);

            // Push the merged tombstone list back if we picked up anything new from either side.
            await WriteJsonToDriveAsync(drive, rootFolderId, TombstonesFileName, mergedTombstones, cancellationToken);

            var newVersion = manifest.Version;
            if (didPushAnything)
            {
                newVersion = manifest.Version + 1;
                await WriteJsonToDriveAsync(drive, rootFolderId, ManifestFileName,
                    new SyncManifest { Version = newVersion, UpdatedByDeviceId = state.DeviceId, UpdatedAtUtc = DateTime.UtcNow },
                    cancellationToken);
            }

            SaveConflicts(conflicts);

            state.LastSyncedVersion = newVersion;
            state.LastSyncedAtUtc = DateTime.UtcNow;
            state.DriveRootFolderId = rootFolderId;
            state.DriveRecipesFolderId = recipesFolderId;
            SaveState(state);

            result.Conflicts = conflicts;
            return result;
        }
        catch (Exception ex)
        {
            return new SyncResult { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            drive.Dispose();
        }
    }

    public async Task<SyncResult> PushOnlyAsync(CancellationToken cancellationToken = default)
    {
        var drive = await _authService.TryGetDriveServiceAsync(cancellationToken);
        if (drive is null)
        {
            return SyncResult.NotConnected;
        }

        var result = new SyncResult { Success = true };

        try
        {
            var state = LoadState();
            if (state.DriveRootFolderId is null || state.DriveRecipesFolderId is null)
            {
                // Never fully synced on this device yet — defer to a full SyncAsync (startup)
                // rather than guessing at folder structure during a best-effort close-time push.
                return new SyncResult { Success = false, ErrorMessage = "Not yet synced on this device." };
            }

            var manifest = await ReadJsonFromDriveAsync<SyncManifest>(drive, state.DriveRootFolderId, ManifestFileName, cancellationToken)
                          ?? new SyncManifest { Version = 0 };

            if (manifest.Version != state.LastSyncedVersion)
            {
                // Drive has moved ahead since our last full sync (another device pushed).
                // Back off rather than risk a silent overwrite with no chance to show a conflict prompt.
                return new SyncResult { Success = false, ErrorMessage = "Drive has newer changes; will reconcile on next full sync." };
            }

            var sinceUtc = state.LastSyncedAtUtc ?? DateTime.MinValue;
            var localTombstones = TombstoneStore.Load();
            var didPushAnything = false;

            didPushAnything |= await PushChangedRecipesOnlyAsync(drive, state.DriveRecipesFolderId, sinceUtc, result, cancellationToken);
            didPushAnything |= await PushDatabaseIfChangedAsync(drive, state.DriveRootFolderId, sinceUtc, result, cancellationToken);
            await WriteJsonToDriveAsync(drive, state.DriveRootFolderId, TombstonesFileName, localTombstones, cancellationToken);

            var newVersion = manifest.Version;
            if (didPushAnything)
            {
                newVersion = manifest.Version + 1;
                await WriteJsonToDriveAsync(drive, state.DriveRootFolderId, ManifestFileName,
                    new SyncManifest { Version = newVersion, UpdatedByDeviceId = state.DeviceId, UpdatedAtUtc = DateTime.UtcNow },
                    cancellationToken);

                state.LastSyncedVersion = newVersion;
                state.LastSyncedAtUtc = DateTime.UtcNow;
                SaveState(state);
            }

            return result;
        }
        catch (Exception ex)
        {
            return new SyncResult { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            drive.Dispose();
        }
    }

    public async Task ResolveConflictKeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
    {
        var drive = await _authService.TryGetDriveServiceAsync(cancellationToken)
                    ?? throw new InvalidOperationException("Google Drive isn't connected.");
        var state = LoadState();

        try
        {
            if (conflict.Kind == SyncConflictKind.Recipe && conflict.RecipeId is not null)
            {
                var recipesFolderId = state.DriveRecipesFolderId
                    ?? await GetOrCreateFolderIdAsync(drive, state, isRecipesFolder: true, cancellationToken);
                await PushRecipeFolderAsync(drive, recipesFolderId, conflict.RecipeId, cancellationToken);
            }
            else if (conflict.Kind == SyncConflictKind.Database)
            {
                var rootFolderId = state.DriveRootFolderId
                    ?? await GetOrCreateFolderIdAsync(drive, state, isRecipesFolder: false, cancellationToken);
                await PushDatabaseAsync(drive, rootFolderId, cancellationToken);
            }

            RemoveConflictAndCleanup(conflict);
        }
        finally
        {
            drive.Dispose();
        }
    }

    public async Task ResolveConflictUseDriveAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
    {
        if (conflict.Kind == SyncConflictKind.Recipe && conflict.RecipeId is not null)
        {
            var destination = AppPaths.RecipeMarkdownFilePath(conflict.RecipeId);
            File.Copy(conflict.ConflictCopyPath, destination, overwrite: true);
        }
        else if (conflict.Kind == SyncConflictKind.Database)
        {
            SqliteConnection.ClearAllPools();
            File.Copy(conflict.ConflictCopyPath, AppPaths.DatabaseFilePath, overwrite: true);
        }

        RemoveConflictAndCleanup(conflict);
        await Task.CompletedTask;
    }

    // ── Recipes ──────────────────────────────────────────────────────────────

    private static async Task<bool> SyncRecipesAsync(
        DriveService drive, string recipesFolderId, TombstoneFile tombstones, DateTime sinceUtc,
        List<SyncConflict> conflicts, SyncResult result, CancellationToken ct)
    {
        var didPush = false;
        var tombstonedIds = tombstones.Recipes.Select(r => r.RecipeId).ToHashSet();
        var alreadyConflicted = conflicts.Where(c => c.Kind == SyncConflictKind.Recipe).Select(c => c.RecipeId).ToHashSet();

        var localRecipeIds = Directory.Exists(AppPaths.RecipesFolder)
            ? Directory.EnumerateDirectories(AppPaths.RecipesFolder).Select(Path.GetFileName).Where(n => n is not null).Select(n => n!).ToList()
            : new List<string>();

        var remoteFolders = await DriveFileHelper.ListChildrenAsync(drive, recipesFolderId, ct);
        var remoteFoldersByName = remoteFolders.ToDictionary(f => f.Name);

        foreach (var recipeId in localRecipeIds)
        {
            if (tombstonedIds.Contains(recipeId) || alreadyConflicted.Contains(recipeId))
            {
                continue;
            }

            var localMdPath = AppPaths.RecipeMarkdownFilePath(recipeId);
            if (!File.Exists(localMdPath))
            {
                continue;
            }

            var localUpdatedAt = File.GetLastWriteTimeUtc(localMdPath);
            var localChanged = localUpdatedAt > sinceUtc;

            if (!remoteFoldersByName.TryGetValue(recipeId, out var remoteFolder))
            {
                // New locally, doesn't exist on Drive yet.
                await PushRecipeFolderAsync(drive, recipesFolderId, recipeId, ct);
                result.RecipesPushed++;
                didPush = true;
                continue;
            }

            var remoteChildren = await DriveFileHelper.ListChildrenAsync(drive, remoteFolder.Id, ct);
            var remoteMd = remoteChildren.FirstOrDefault(f => f.Name == RecipeMarkdownFileName);
            var remoteUpdatedAt = remoteMd?.ModifiedTimeDateTimeOffset?.UtcDateTime ?? DateTime.MinValue;
            var remoteChanged = remoteUpdatedAt > sinceUtc;

            if (!localChanged && !remoteChanged)
            {
                continue;
            }

            if (localChanged && remoteChanged)
            {
                // Same recipe edited on both sides since the last sync — do not guess; keep local
                // untouched and save the Drive version alongside for the user to compare/choose.
                var recipeName = TryReadRecipeName(localMdPath) ?? recipeId;
                var conflictPath = Path.Combine(AppPaths.ConflictsFolder,
                    $"{SanitizeFileName(recipeName)} (Drive conflict copy {DateTime.Now:yyyy-MM-dd HHmm}).md");

                if (remoteMd is not null)
                {
                    await DownloadFileToPathAsync(drive, remoteMd.Id, conflictPath, ct);
                }

                conflicts.Add(new SyncConflict
                {
                    Kind = SyncConflictKind.Recipe,
                    RecipeId = recipeId,
                    DisplayName = recipeName,
                    ConflictCopyPath = conflictPath
                });
                continue;
            }

            if (remoteChanged)
            {
                await PullRecipeFolderAsync(drive, remoteFolder.Id, recipeId, ct);
                result.RecipesPulled++;
            }
            else
            {
                await PushRecipeFolderAsync(drive, recipesFolderId, recipeId, ct);
                result.RecipesPushed++;
                didPush = true;
            }
        }

        // Remote recipes that don't exist locally yet (new from another device) and aren't tombstoned.
        foreach (var remoteFolder in remoteFolders)
        {
            if (localRecipeIds.Contains(remoteFolder.Name) || tombstonedIds.Contains(remoteFolder.Name))
            {
                continue;
            }

            await PullRecipeFolderAsync(drive, remoteFolder.Id, remoteFolder.Name, ct);
            result.RecipesPulled++;
        }

        return didPush;
    }

    private static async Task<bool> PushChangedRecipesOnlyAsync(
        DriveService drive, string recipesFolderId, DateTime sinceUtc, SyncResult result, CancellationToken ct)
    {
        if (!Directory.Exists(AppPaths.RecipesFolder))
        {
            return false;
        }

        var didPush = false;
        var tombstonedIds = TombstoneStore.Load().Recipes.Select(r => r.RecipeId).ToHashSet();

        foreach (var folder in Directory.EnumerateDirectories(AppPaths.RecipesFolder))
        {
            var recipeId = Path.GetFileName(folder)!;
            if (tombstonedIds.Contains(recipeId))
            {
                continue;
            }

            var mdPath = AppPaths.RecipeMarkdownFilePath(recipeId);
            if (File.Exists(mdPath) && File.GetLastWriteTimeUtc(mdPath) > sinceUtc)
            {
                await PushRecipeFolderAsync(drive, recipesFolderId, recipeId, ct);
                result.RecipesPushed++;
                didPush = true;
            }
        }

        return didPush;
    }

    private static async Task PushRecipeFolderAsync(DriveService drive, string recipesFolderId, string recipeId, CancellationToken ct)
    {
        var remoteFolderId = await DriveFileHelper.FindOrCreateFolderAsync(drive, recipeId, recipesFolderId, ct);
        var localFolder = AppPaths.RecipeFolder(recipeId);

        foreach (var filePath in Directory.EnumerateFiles(localFolder))
        {
            await using var stream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            await DriveFileHelper.UploadOrUpdateFileAsync(drive, fileName, remoteFolderId, stream, GetMimeType(fileName), ct);
        }
    }

    private static async Task PullRecipeFolderAsync(DriveService drive, string remoteFolderId, string recipeId, CancellationToken ct)
    {
        var localFolder = AppPaths.RecipeFolder(recipeId);
        var remoteChildren = await DriveFileHelper.ListChildrenAsync(drive, remoteFolderId, ct);

        foreach (var remoteFile in remoteChildren)
        {
            var destination = Path.Combine(localFolder, remoteFile.Name);
            await DownloadFileToPathAsync(drive, remoteFile.Id, destination, ct);
        }
    }

    private static string? TryReadRecipeName(string mdPath)
    {
        try
        {
            var content = File.ReadAllText(mdPath);
            var nameLine = content.Split('\n').FirstOrDefault(l => l.TrimStart().StartsWith("name:"));
            return nameLine?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
        }
        catch (IOException)
        {
            return null;
        }
    }

    // ── Database (whole-file unit) ──────────────────────────────────────────

    private static async Task<bool> SyncDatabaseAsync(
        DriveService drive, string rootFolderId, DateTime sinceUtc,
        List<SyncConflict> conflicts, SyncResult result, CancellationToken ct)
    {
        if (conflicts.Any(c => c.Kind == SyncConflictKind.Database))
        {
            return false;
        }

        if (!File.Exists(AppPaths.DatabaseFilePath))
        {
            return false;
        }

        var localUpdatedAt = File.GetLastWriteTimeUtc(AppPaths.DatabaseFilePath);
        var localChanged = localUpdatedAt > sinceUtc;

        var remoteDb = await DriveFileHelper.FindChildAsync(drive, DatabaseFileName, rootFolderId, ct);
        if (remoteDb is null)
        {
            if (!localChanged)
            {
                return false;
            }

            await PushDatabaseAsync(drive, rootFolderId, ct);
            result.DatabasePushed = true;
            return true;
        }

        var remoteUpdatedAt = remoteDb.ModifiedTimeDateTimeOffset?.UtcDateTime ?? DateTime.MinValue;
        var remoteChanged = remoteUpdatedAt > sinceUtc;

        if (!localChanged && !remoteChanged)
        {
            return false;
        }

        if (localChanged && remoteChanged)
        {
            var conflictPath = Path.Combine(AppPaths.ConflictsFolder, $"broccoli (Drive conflict copy {DateTime.Now:yyyy-MM-dd HHmm}).db");
            await DownloadFileToPathAsync(drive, remoteDb.Id, conflictPath, ct);
            conflicts.Add(new SyncConflict
            {
                Kind = SyncConflictKind.Database,
                DisplayName = "App data (grocery list, pantry, meal plans, macros)",
                ConflictCopyPath = conflictPath
            });
            return false;
        }

        if (remoteChanged)
        {
            SqliteConnection.ClearAllPools();
            await DownloadFileToPathAsync(drive, remoteDb.Id, AppPaths.DatabaseFilePath, ct);
            result.DatabasePulled = true;
            return false;
        }

        await PushDatabaseAsync(drive, rootFolderId, ct);
        result.DatabasePushed = true;
        return true;
    }

    private static async Task<bool> PushDatabaseIfChangedAsync(
        DriveService drive, string rootFolderId, DateTime sinceUtc, SyncResult result, CancellationToken ct)
    {
        if (!File.Exists(AppPaths.DatabaseFilePath) || File.GetLastWriteTimeUtc(AppPaths.DatabaseFilePath) <= sinceUtc)
        {
            return false;
        }

        await PushDatabaseAsync(drive, rootFolderId, ct);
        result.DatabasePushed = true;
        return true;
    }

    private static async Task PushDatabaseAsync(DriveService drive, string rootFolderId, CancellationToken ct)
    {
        // Take a clean, consistent snapshot regardless of journal mode (WAL, etc.) rather than
        // uploading the live file, which could be mid-write.
        var snapshotPath = Path.Combine(Path.GetTempPath(), $"broccoli-snapshot-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={AppPaths.DatabaseFilePath}"))
            {
                await connection.OpenAsync(ct);
                await using var command = connection.CreateCommand();
                command.CommandText = "VACUUM INTO $path";
                command.Parameters.AddWithValue("$path", snapshotPath);
                await command.ExecuteNonQueryAsync(ct);
            }

            await using var stream = File.OpenRead(snapshotPath);
            await DriveFileHelper.UploadOrUpdateFileAsync(drive, DatabaseFileName, rootFolderId, stream, "application/octet-stream", ct);
        }
        finally
        {
            if (File.Exists(snapshotPath))
            {
                File.Delete(snapshotPath);
            }
        }
    }

    // ── Tombstones ───────────────────────────────────────────────────────────

    private static async Task<bool> SyncTombstonesAsync(
        DriveService drive, string recipesFolderId, TombstoneFile mergedTombstones, CancellationToken ct)
    {
        var remoteFolders = await DriveFileHelper.ListChildrenAsync(drive, recipesFolderId, ct);
        var remoteFoldersByName = remoteFolders.ToDictionary(f => f.Name);
        var anyRemoteDeleted = false;

        foreach (var tombstone in mergedTombstones.Recipes)
        {
            var localFolder = Path.Combine(AppPaths.RecipesFolder, tombstone.RecipeId);
            if (Directory.Exists(localFolder))
            {
                Directory.Delete(localFolder, recursive: true);
            }

            if (remoteFoldersByName.TryGetValue(tombstone.RecipeId, out var remoteFolder))
            {
                await DriveFileHelper.TrashFileAsync(drive, remoteFolder.Id, ct);
                anyRemoteDeleted = true;
            }
        }

        return anyRemoteDeleted;
    }

    // ── Folder bootstrap ─────────────────────────────────────────────────────

    private static async Task<string> GetOrCreateFolderIdAsync(
        DriveService drive, SyncState state, bool isRecipesFolder, CancellationToken ct)
    {
        if (!isRecipesFolder && state.DriveRootFolderId is not null)
        {
            return state.DriveRootFolderId;
        }
        if (isRecipesFolder && state.DriveRecipesFolderId is not null)
        {
            return state.DriveRecipesFolderId;
        }

        var rootId = state.DriveRootFolderId ?? await DriveFileHelper.FindOrCreateFolderAsync(drive, "Broccoli", null, ct);
        if (!isRecipesFolder)
        {
            return rootId;
        }

        return await DriveFileHelper.FindOrCreateFolderAsync(drive, RecipesFolderName, rootId, ct);
    }

    // ── Conflict bookkeeping ─────────────────────────────────────────────────

    private static void RemoveConflictAndCleanup(SyncConflict conflict)
    {
        var conflicts = LoadConflicts();
        conflicts.RemoveAll(c => c.ConflictCopyPath == conflict.ConflictCopyPath && c.Kind == conflict.Kind);
        SaveConflicts(conflicts);

        if (File.Exists(conflict.ConflictCopyPath))
        {
            File.Delete(conflict.ConflictCopyPath);
        }
    }

    private static string ConflictsIndexPath => Path.Combine(AppPaths.ConflictsFolder, "conflicts-index.json");

    private static List<SyncConflict> LoadConflicts()
    {
        if (!File.Exists(ConflictsIndexPath))
        {
            return new List<SyncConflict>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<SyncConflict>>(File.ReadAllText(ConflictsIndexPath)) ?? new List<SyncConflict>();
        }
        catch (JsonException)
        {
            return new List<SyncConflict>();
        }
    }

    private static void SaveConflicts(List<SyncConflict> conflicts) =>
        File.WriteAllText(ConflictsIndexPath, JsonSerializer.Serialize(conflicts, JsonOptions));

    private static SyncState LoadState()
    {
        if (!File.Exists(AppPaths.SyncStateFilePath))
        {
            return new SyncState();
        }

        try
        {
            return JsonSerializer.Deserialize<SyncState>(File.ReadAllText(AppPaths.SyncStateFilePath)) ?? new SyncState();
        }
        catch (JsonException)
        {
            return new SyncState();
        }
    }

    private static void SaveState(SyncState state) =>
        File.WriteAllText(AppPaths.SyncStateFilePath, JsonSerializer.Serialize(state, JsonOptions));

    // ── Small JSON/file helpers over Drive ──────────────────────────────────

    private static async Task<T?> ReadJsonFromDriveAsync<T>(DriveService drive, string parentId, string fileName, CancellationToken ct)
    {
        var file = await DriveFileHelper.FindChildAsync(drive, fileName, parentId, ct);
        if (file is null)
        {
            return default;
        }

        await using var stream = await DriveFileHelper.DownloadFileAsync(drive, file.Id, ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(ct);
        return JsonSerializer.Deserialize<T>(json);
    }

    private static async Task WriteJsonToDriveAsync<T>(DriveService drive, string parentId, string fileName, T value, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await DriveFileHelper.UploadOrUpdateFileAsync(drive, fileName, parentId, stream, "application/json", ct);
    }

    private static async Task DownloadFileToPathAsync(DriveService drive, string fileId, string destinationPath, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var remoteStream = await DriveFileHelper.DownloadFileAsync(drive, fileId, ct);
        await using var fileStream = File.Create(destinationPath);
        await remoteStream.CopyToAsync(fileStream, ct);
    }

    private static string GetMimeType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".md" => "text/markdown",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }
        return name;
    }
}
