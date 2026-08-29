using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// Singleton observable wrapper around <see cref="IGoogleDriveSyncService"/> (see
/// <see cref="ISyncStatusService"/>). Wraps every sync/push call so that in-progress, result, and
/// dirty state are reflected app-wide regardless of which trigger started the operation.
/// </summary>
public partial class SyncStatusService : ObservableObject, ISyncStatusService
{
    private static readonly SyncResult AlreadySyncingResult = new()
    {
        Success = false,
        ErrorMessage = "A sync is already in progress.",
    };

    private readonly IGoogleDriveSyncService _syncService;
    private readonly IGoogleDriveAuthService _authService;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private DateTime? _lastSyncedAtUtc;

    [ObservableProperty]
    private bool _hasUnsyncedChanges;

    [ObservableProperty]
    private string? _lastSyncError;

    public SyncStatusService(IGoogleDriveSyncService syncService, IGoogleDriveAuthService authService)
    {
        _syncService = syncService;
        _authService = authService;

        // StorageChangedMessage is broadcast by the sync service itself after every successful
        // sync, so even syncs started from Settings (or the shutdown push) refresh this state.
        WeakReferenceMessenger.Default.Register<StorageChangedMessage>(this, (_, _) => RefreshStatus());
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        IsConnected = _authService.GetStoredAccount() is not null;
        LastSyncedAtUtc = _syncService.LastSyncedAtUtc;
        HasUnsyncedChanges = IsConnected && _syncService.HasPendingChanges();
    }

    public async Task<SyncResult> SyncNowAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (IsSyncing)
        {
            return AlreadySyncingResult;
        }

        IsSyncing = true;
        LastSyncError = null;
        try
        {
            SyncResult result = await _syncService.SyncAsync(progress, cancellationToken);
            if (!result.Success)
            {
                LastSyncError = result.ErrorMessage;
            }

            return result;
        }
        finally
        {
            IsSyncing = false;
            RefreshStatus();
        }
    }

    public async Task<SyncResult> PushOnlyAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (IsSyncing)
        {
            return AlreadySyncingResult;
        }

        IsSyncing = true;
        LastSyncError = null;
        try
        {
            SyncResult result = await _syncService.PushOnlyAsync(progress, cancellationToken);
            if (!result.Success)
            {
                LastSyncError = result.ErrorMessage;
            }

            return result;
        }
        finally
        {
            IsSyncing = false;
            RefreshStatus();
        }
    }

    public IReadOnlyList<SyncConflict> GetPendingConflicts() => _syncService.GetPendingConflicts();

    public Task ResolveConflictKeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
        => _syncService.ResolveConflictKeepLocalAsync(conflict, cancellationToken);

    public Task ResolveConflictUseDriveAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
        => _syncService.ResolveConflictUseDriveAsync(conflict, cancellationToken);
}
