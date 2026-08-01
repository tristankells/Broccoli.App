using System.Collections.ObjectModel;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Settings.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IGoogleDriveAuthService _googleDriveAuthService;
    private readonly IGoogleDriveSyncService _googleDriveSyncService;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string? _connectedEmail;

    [ObservableProperty]
    private DateTime? _connectedAtUtc;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private DateTime? _lastSyncedAtUtc;

    [ObservableProperty]
    private string? _syncStatusMessage;

    public ObservableCollection<SyncConflict> Conflicts { get; } = new();

    /// <summary>
    /// Explicit bool for the UI to bind visibility to, rather than relying on Avalonia's loose
    /// int-&gt;bool binding conversion on <c>Conflicts.Count</c>.
    /// </summary>
    [ObservableProperty]
    private bool _hasConflicts;

    /// <summary>
    /// Exposed so the app shell can trigger the best-effort push-on-close sync directly
    /// (bypassing the UI-bound <see cref="SyncNowCommand"/>, since the app may be shutting down).
    /// </summary>
    public IGoogleDriveSyncService SyncService => _googleDriveSyncService;

    public SettingsViewModel() : this(new GoogleDriveAuthService())
    {
    }

    public SettingsViewModel(IGoogleDriveAuthService googleDriveAuthService)
        : this(googleDriveAuthService, new GoogleDriveSyncService(googleDriveAuthService))
    {
    }

    public SettingsViewModel(IGoogleDriveAuthService googleDriveAuthService, IGoogleDriveSyncService googleDriveSyncService)
    {
        _googleDriveAuthService = googleDriveAuthService;
        _googleDriveSyncService = googleDriveSyncService;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var account = _googleDriveAuthService.GetStoredAccount();
        IsConnected = account is not null;
        ConnectedEmail = account?.Email;
        ConnectedAtUtc = account?.ConnectedAtUtc;
        LastSyncedAtUtc = _googleDriveSyncService.LastSyncedAtUtc;

        RefreshConflicts();
    }

    private void RefreshConflicts()
    {
        Conflicts.Clear();
        foreach (var conflict in _googleDriveSyncService.GetPendingConflicts())
        {
            Conflicts.Add(conflict);
        }
        HasConflicts = Conflicts.Count > 0;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        ErrorMessage = null;
        IsConnecting = true;
        try
        {
            var account = await _googleDriveAuthService.ConnectAsync();
            IsConnected = true;
            ConnectedEmail = account.Email;
            ConnectedAtUtc = account.ConnectedAtUtc;

            // Kick off an initial sync right away so the user sees it actually working.
            await SyncNowAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        ErrorMessage = null;
        IsConnecting = true;
        try
        {
            await _googleDriveAuthService.DisconnectAsync();
            RefreshStatus();
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        ErrorMessage = null;
        SyncStatusMessage = null;
        IsSyncing = true;
        try
        {
            var result = await _googleDriveSyncService.SyncAsync();
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
            else
            {
                SyncStatusMessage = result.HasConflicts
                    ? $"Synced with {result.Conflicts.Count} conflict(s) needing your attention."
                    : "Synced.";
            }

            LastSyncedAtUtc = _googleDriveSyncService.LastSyncedAtUtc;
            RefreshConflicts();
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task KeepLocalAsync(SyncConflict conflict)
    {
        await _googleDriveSyncService.ResolveConflictKeepLocalAsync(conflict);
        Conflicts.Remove(conflict);
        HasConflicts = Conflicts.Count > 0;
    }

    [RelayCommand]
    private async Task UseDriveAsync(SyncConflict conflict)
    {
        await _googleDriveSyncService.ResolveConflictUseDriveAsync(conflict);
        Conflicts.Remove(conflict);
        HasConflicts = Conflicts.Count > 0;
    }
}

