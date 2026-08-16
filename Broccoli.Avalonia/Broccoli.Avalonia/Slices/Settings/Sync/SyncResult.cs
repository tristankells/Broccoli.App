namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>Outcome of a <see cref="IGoogleDriveSyncService"/> sync/push run, shown in Settings.</summary>
public class SyncResult
{
    public static SyncResult NotConnected { get; } = new() { Success = false, ErrorMessage = "Google Drive isn't connected." };

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public List<SyncConflict> Conflicts { get; set; } = new();

    public int RecipesPulled { get; set; }

    public int RecipesPushed { get; set; }

    public bool DatabasePulled { get; set; }

    public bool DatabasePushed { get; set; }

    public bool HasConflicts => Conflicts.Count > 0;
}
