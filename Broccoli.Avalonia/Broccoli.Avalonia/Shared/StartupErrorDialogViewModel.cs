using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Shared;

/// <summary>
/// View model for the dialog shown when the app can't initialize its local database at startup
/// (migration failure, locked/corrupt file, etc.). Gives the user the path to the database,
/// actionable troubleshooting steps, and a Retry that re-runs the startup load.
/// </summary>
public partial class StartupErrorDialogViewModel : ViewModelBase
{
    /// <summary>Title shown in the dialog's title bar.</summary>
    public string Title { get; set; } = "Broccoli couldn't start its database";

    /// <summary>Full path to the SQLite database file (for the troubleshooting message).</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>Raw exception details shown in a read-only box for advanced troubleshooting.</summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>Called by the Retry button; returns true when startup loading succeeded.</summary>
    public Func<Task<bool>>? RetryAction { get; set; }

    /// <summary>Opens the app-data folder in the platform file browser.</summary>
    public Action? OpenDataFolderAction { get; set; }

    /// <summary>Closes the dialog.</summary>
    public Action? RequestClose { get; set; }

    /// <summary>Message shown after a Retry attempt fails again.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRetryError))]
    private string? _retryError;

    /// <summary>True while a Retry attempt is running (disables the buttons).</summary>
    [ObservableProperty]
    private bool _isRetrying;

    /// <summary>True when <see cref="RetryError"/> is set, so the message is visible.</summary>
    public bool HasRetryError => RetryError is not null;

    public string Message =>
        "Broccoli couldn't open its local database, so your data can't be loaded yet.\n\n" +
        $"The database file is:\n{DatabasePath}\n\n" +
        "Common causes:\n" +
        "• Another copy of Broccoli is still running and has the file locked.\n" +
        "• The drive is offline, read-only, or out of space.\n" +
        "• The database was damaged or left in an unfinished state.\n\n" +
        "What to try:\n" +
        "• Close any other running copies of Broccoli, then click Retry.\n" +
        "• Open the data folder and check the file isn't read-only.\n" +
        "• As a last resort, quit Broccoli, rename or delete \"broccoli.db\" (your recipes are " +
        "stored separately as Markdown and are kept), then restart the app.";

    [RelayCommand]
    private async Task Retry()
    {
        if (RetryAction is null)
        {
            return;
        }

        IsRetrying = true;
        RetryError = null;
        try
        {
            bool succeeded = await RetryAction();
            if (succeeded)
            {
                RequestClose?.Invoke();
            }
            else
            {
                RetryError = "Loading still isn't working. Try the steps above, then Retry again.";
            }
        }
        finally
        {
            IsRetrying = false;
        }
    }

    [RelayCommand]
    private void OpenDataFolder() => OpenDataFolderAction?.Invoke();

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();
}
