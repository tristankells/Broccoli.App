using Broccoli.Avalonia.Slices.Settings.Sync;
using Broccoli.Avalonia.Storage;
using Google.Apis.Drive.v3;

namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// Handles the Google Drive "sign in for backup" flow: an OAuth "installed app" login that opens
/// the system browser, scoped to only the files this app itself creates (<c>drive.file</c>), plus
/// reading/clearing the locally-connected account. The redirect is captured by the platform-specific
/// code receiver supplied via <see cref="IGoogleDriveOAuthPlatform"/> (loopback on desktop, custom
/// URI scheme on mobile); desktop also supplies a client secret, while mobile relies on PKCE only.
/// </summary>
public interface IGoogleDriveAuthService
{
    /// <summary>
    /// Returns the currently-connected account from local storage, or null if Drive backup
    /// has never been connected (or was disconnected). Does not make a network call.
    /// </summary>
    GoogleDriveAccountInfo? GetStoredAccount();

    /// <summary>
    /// Runs the OAuth login flow (opens the system browser), then records and returns the
    /// connected Google account. Throws <see cref="InvalidOperationException"/> if no OAuth
    /// client id is available (neither the platform-embedded id nor a user-supplied override at
    /// <see cref="AppPaths.GoogleDriveOAuthConfigFilePath"/>). Reports progress via
    /// <paramref name="progress"/>.
    /// </summary>
    Task<GoogleDriveAccountInfo> ConnectAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Signs out: clears the cached OAuth token and the locally-recorded account.</summary>
    Task DisconnectAsync();

    /// <summary>
    /// Returns an authorized <see cref="DriveService"/> using the cached OAuth token if this
    /// device has previously connected (silently refreshing the token if needed, without
    /// opening the browser). Returns null if Drive backup isn't connected on this device.
    /// </summary>
    Task<DriveService?> TryGetDriveServiceAsync(CancellationToken cancellationToken = default);
}
