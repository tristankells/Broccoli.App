using System.Collections.ObjectModel;
using Avalonia.Threading;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Shell;

/// <summary>
/// Backs the narrow, right-aligned storage-usage footer at the bottom of the shell. Shows the
/// app's total footprint at a glance, with a per-category breakdown (Markdown recipes, images,
/// history backups, database) plus the Drive quota revealed on hover.
/// </summary>
public partial class StorageUsageFooterViewModel : ViewModelBase
{
    private readonly IStorageUsageService _storageUsageService;
    private readonly IGoogleDriveAuthService _authService;
    private CancellationTokenSource? _refreshCts;

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private bool _hasDriveQuota;

    public StorageUsageFooterViewModel()
        : this(new GoogleDriveAuthService(new DesktopGoogleDriveOAuthPlatform()))
    {
    }

    public StorageUsageFooterViewModel(IGoogleDriveAuthService authService)
        : this(new StorageUsageService(authService), authService)
    {
    }

    public StorageUsageFooterViewModel(
        IStorageUsageService storageUsageService,
        IGoogleDriveAuthService authService)
    {
        _storageUsageService = storageUsageService;
        _authService = authService;

        WeakReferenceMessenger.Default.Register<StorageChangedMessage>(this, (_, _) => ScheduleRefresh());
    }

    public ObservableCollection<StorageBreakdownItem> BreakdownItems { get; } = new();

    /// <summary>
    /// Debounces <see cref="RefreshAsync"/>: StorageChangedMessage now fires on every local data
    /// change (grocery toggles, recipe saves, ...), so consecutive changes collapse into one
    /// refresh shortly after the last one instead of scanning the store per change.
    /// </summary>
    private void ScheduleRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        _ = DebouncedRefreshAsync(_refreshCts.Token);
    }

    private async Task DebouncedRefreshAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(800), token);
            await RefreshAsync();
        }
        catch (OperationCanceledException)
        {
            // A newer storage change superseded this refresh.
        }
    }

    public async Task RefreshAsync()
    {
        StorageUsageSnapshot snapshot = _storageUsageService.ComputeLocalUsage();
        DriveQuota? quota = _authService.GetStoredAccount() is null
            ? null
            : await _storageUsageService.GetDriveQuotaAsync();

        await Dispatcher.UIThread.InvokeAsync(() => Apply(snapshot, quota));
    }

    private void Apply(StorageUsageSnapshot snapshot, DriveQuota? quota)
    {
        HasDriveQuota = quota is not null;

        SummaryText = quota is null
            ? $"Using {FormatBytes(snapshot.TotalBytes)}"
            : $"Using {FormatBytes(snapshot.TotalBytes)} · {FormatBytes(quota.RemainingBytes)} free on Drive";

        BreakdownItems.Clear();
        BreakdownItems.Add(new StorageBreakdownItem("Markdown recipes", FormatBytes(snapshot.MarkdownBytes)));
        BreakdownItems.Add(new StorageBreakdownItem("Images", FormatBytes(snapshot.ImageBytes)));
        BreakdownItems.Add(new StorageBreakdownItem("Backups", FormatBytes(snapshot.BackupBytes)));
        BreakdownItems.Add(new StorageBreakdownItem("Database", FormatBytes(snapshot.DatabaseBytes)));
        BreakdownItems.Add(new StorageBreakdownItem("Total", FormatBytes(snapshot.TotalBytes)));

        if (quota is not null)
        {
            BreakdownItems.Add(new StorageBreakdownItem(
                "Google Drive",
                $"{FormatBytes(quota.UsedBytes)} of {FormatBytes(quota.LimitBytes)}"));
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{value:0.#} {units[unit]}";
    }
}

/// <summary>Single label/value row shown in the footer's hover breakdown.</summary>
public sealed class StorageBreakdownItem
{
    public StorageBreakdownItem(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public string Value { get; }
}
