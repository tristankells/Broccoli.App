namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// A single progress update emitted while connecting or syncing, bound to the UI.
/// <see cref="Progress"/> is a fraction in [0,1] for a determinate progress bar, or null for an
/// indeterminate (busy) bar — used while waiting on the user (browser sign-in / consent).
/// </summary>
public sealed class SyncProgress
{
    public string Message { get; init; } = string.Empty;

    public double? Progress { get; init; }

    /// <summary>Reports a progress update to an optional <see cref="IProgress{T}"/> sink (no-op when null).</summary>
    public static void Report(IProgress<SyncProgress>? progress, string message, double? fraction = null)
        => progress?.Report(new SyncProgress { Message = message, Progress = fraction });
}
