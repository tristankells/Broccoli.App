using System.Text.Json;
using Broccoli.Avalonia.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// Handles the Google Drive "sign in for backup" flow: an OAuth "installed app" login
/// (opens the system browser via a local loopback listener), scoped to only the files this
/// app itself creates (<c>drive.file</c>), plus reading/clearing the locally-connected account.
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
    /// client id/secret has been configured yet (see <see cref="AppPaths.GoogleDriveOAuthConfigFilePath"/>).
    /// </summary>
    Task<GoogleDriveAccountInfo> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Signs out: clears the cached OAuth token and the locally-recorded account.</summary>
    Task DisconnectAsync();

    /// <summary>
    /// Returns an authorized <see cref="DriveService"/> using the cached OAuth token if this
    /// device has previously connected (silently refreshing the token if needed, without
    /// opening the browser). Returns null if Drive backup isn't connected on this device.
    /// </summary>
    Task<DriveService?> TryGetDriveServiceAsync(CancellationToken cancellationToken = default);
}

public class GoogleDriveAuthService : IGoogleDriveAuthService
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public GoogleDriveAccountInfo? GetStoredAccount()
    {
        var path = AppPaths.GoogleDriveAccountFilePath;
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GoogleDriveAccountInfo>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            // Corrupt/partial file — treat as not connected rather than crashing the settings popup.
            return null;
        }
    }

    public async Task<GoogleDriveAccountInfo> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var oauthOptions = LoadOAuthOptions();
        if (!oauthOptions.IsConfigured)
        {
            throw new InvalidOperationException(
                "Google Drive backup isn't configured yet. Add your OAuth client id/secret to " +
                $"\"{AppPaths.GoogleDriveOAuthConfigFilePath}\" (create an OAuth client of type " +
                "\"Desktop app\" in Google Cloud Console), then try again.");
        }

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets { ClientId = oauthOptions.ClientId, ClientSecret = oauthOptions.ClientSecret },
            Scopes,
            "user",
            cancellationToken,
            new FileDataStore(AppPaths.GoogleDriveTokenFolder, fullPath: true));

        using var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Broccoli"
        });

        var aboutRequest = driveService.About.Get();
        aboutRequest.Fields = "user";
        var about = await aboutRequest.ExecuteAsync(cancellationToken);

        var account = new GoogleDriveAccountInfo
        {
            Email = about.User.EmailAddress,
            ConnectedAtUtc = DateTime.UtcNow
        };

        File.WriteAllText(AppPaths.GoogleDriveAccountFilePath, JsonSerializer.Serialize(account, JsonOptions));
        return account;
    }

    public Task DisconnectAsync()
    {
        if (Directory.Exists(AppPaths.GoogleDriveTokenFolder))
        {
            Directory.Delete(AppPaths.GoogleDriveTokenFolder, recursive: true);
        }

        if (File.Exists(AppPaths.GoogleDriveAccountFilePath))
        {
            File.Delete(AppPaths.GoogleDriveAccountFilePath);
        }

        return Task.CompletedTask;
    }

    public async Task<DriveService?> TryGetDriveServiceAsync(CancellationToken cancellationToken = default)
    {
        // Only attempt silent token refresh if we've actually connected before — avoids ever
        // popping a browser window from background sync triggers (startup/close).
        if (GetStoredAccount() is null)
        {
            return null;
        }

        var oauthOptions = LoadOAuthOptions();
        if (!oauthOptions.IsConfigured)
        {
            return null;
        }

        try
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets { ClientId = oauthOptions.ClientId, ClientSecret = oauthOptions.ClientSecret },
                Scopes,
                "user",
                cancellationToken,
                new FileDataStore(AppPaths.GoogleDriveTokenFolder, fullPath: true));

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Broccoli"
            });
        }
        catch (Exception)
        {
            // Cached token missing/revoked/expired beyond refresh — treat as not connected.
            // The user will see "Not connected" in Settings and can reconnect explicitly.
            return null;
        }
    }

    private static GoogleDriveOAuthOptions LoadOAuthOptions()
    {
        var path = AppPaths.GoogleDriveOAuthConfigFilePath;
        if (!File.Exists(path))
        {
            // Seed an empty template so the user knows exactly where to add their credentials.
            File.WriteAllText(path, JsonSerializer.Serialize(new GoogleDriveOAuthOptions(), JsonOptions));
            return new GoogleDriveOAuthOptions();
        }

        try
        {
            return JsonSerializer.Deserialize<GoogleDriveOAuthOptions>(File.ReadAllText(path))
                   ?? new GoogleDriveOAuthOptions();
        }
        catch (JsonException)
        {
            return new GoogleDriveOAuthOptions();
        }
    }
}
