namespace Broccoli.Avalonia.Services;

/// <summary>
/// Records which Google account (if any) has been connected for Drive backup.
/// Persisted locally at <see cref="Storage.AppPaths.GoogleDriveAccountFilePath"/>.
/// The actual OAuth token is managed separately by Google.Apis.Auth's FileDataStore
/// under <see cref="Storage.AppPaths.GoogleDriveTokenFolder"/>.
/// </summary>
public class GoogleDriveAccountInfo
{
    public string Email { get; set; } = string.Empty;
    public DateTime ConnectedAtUtc { get; set; }
}
