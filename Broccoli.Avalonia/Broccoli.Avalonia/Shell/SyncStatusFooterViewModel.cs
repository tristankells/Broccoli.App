using System.Globalization;
using Avalonia.Threading;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Shell;

/// <summary>
/// Backs the left-hand sync-status section of the shell footer: a clickable summary that shows the
/// last-synced time, flags local changes not yet pushed, and reveals an animated indicator while a
/// sync is running. Mirrors the shared <see cref="ISyncStatusService"/> so syncs triggered from
/// anywhere (startup, Settings, this footer) are reflected here.
/// </summary>
public partial class SyncStatusFooterViewModel : ViewModelBase
{
    private readonly ISyncStatusService _syncStatusService;

    public SyncStatusFooterViewModel()
        : this(new SyncStatusService(
            new GoogleDriveSyncService(new GoogleDriveAuthService(new DesktopGoogleDriveOAuthPlatform())),
            new GoogleDriveAuthService(new DesktopGoogleDriveOAuthPlatform())))
    {
    }

    public SyncStatusFooterViewModel(ISyncStatusService syncStatusService)
    {
        _syncStatusService = syncStatusService;
        _syncStatusService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ISyncStatusService.IsSyncing)
                or nameof(ISyncStatusService.IsConnected)
                or nameof(ISyncStatusService.LastSyncedAtUtc)
                or nameof(ISyncStatusService.HasUnsyncedChanges))
            {
                OnPropertyChanged(nameof(SummaryText));
                OnPropertyChanged(nameof(ShowSyncIcon));
                OnPropertyChanged(nameof(ShowEnableHint));
            }
        };

        // The "synced X" relative time ages even while nothing changes, so re-evaluate the
        // summary periodically to keep it fresh (e.g. "just now" -> "5m ago").
        var refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        refreshTimer.Tick += (_, _) => OnPropertyChanged(nameof(SummaryText));
        refreshTimer.Start();
    }

    /// <summary>The shared, observable sync state this footer mirrors.</summary>
    public ISyncStatusService SyncStatus => _syncStatusService;

    /// <summary>True to show the static sync glyph instead of the in-progress spinner.</summary>
    public bool ShowSyncIcon => _syncStatusService.IsConnected && !_syncStatusService.IsSyncing;

    /// <summary>True when Drive isn't connected — used to hint at enabling backup in Settings.</summary>
    public bool ShowEnableHint => !_syncStatusService.IsConnected;

    /// <summary>Human-readable one-liner shown in the footer.</summary>
    public string SummaryText => _syncStatusService.IsSyncing
        ? "Syncing to Drive…"
        : !_syncStatusService.IsConnected
            ? "Backup off"
            : _syncStatusService.HasUnsyncedChanges
                ? $"Unsynced changes · synced {FormatLastSynced()}"
                : $"Synced {FormatLastSynced()}";

    [RelayCommand]
    private Task SyncNowAsync() => _syncStatusService.SyncNowAsync();

    private string FormatLastSynced()
    {
        DateTime? lastSyncedUtc = _syncStatusService.LastSyncedAtUtc;
        if (lastSyncedUtc is null)
        {
            return "never";
        }

        TimeSpan elapsed = DateTime.UtcNow - lastSyncedUtc.Value;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{(int)elapsed.TotalMinutes}m ago";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return $"{(int)elapsed.TotalHours}h ago";
        }

        if (elapsed < TimeSpan.FromDays(7))
        {
            return $"{(int)elapsed.TotalDays}d ago";
        }

        return lastSyncedUtc.Value.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
    }
}
