namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>Byte counts for the four kinds of data the app stores, plus their total.</summary>
public sealed record StorageUsageSnapshot(
    long MarkdownBytes,
    long ImageBytes,
    long BackupBytes,
    long DatabaseBytes)
{
    public long TotalBytes => MarkdownBytes + ImageBytes + BackupBytes + DatabaseBytes;
}

/// <summary>The connected Google account's overall Drive usage, as reported by the Drive API.</summary>
public sealed record DriveQuota(long UsedBytes, long LimitBytes)
{
    public long RemainingBytes => Math.Max(0, LimitBytes - UsedBytes);
}
