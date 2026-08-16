using System.Text.Json;
using Broccoli.Avalonia.Slices.Settings.Sync;
using Broccoli.Avalonia.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Broccoli.Avalonia.Slices.Settings;

public class GoogleDriveAuthService : IGoogleDriveAuthService
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IGoogleDriveOAuthPlatform _platform;

    public GoogleDriveAuthService(IGoogleDriveOAuthPlatform platform)
    {
        _platform = platform;
    }

    public GoogleDriveAccountInfo? GetStoredAccount()
    {
        string path = AppPaths.GoogleDriveAccountFilePath;
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

    public async Task<GoogleDriveAccountInfo> ConnectAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        GoogleDriveOAuthOptions oauthOptions = LoadOAuthOptions();
        if (!oauthOptions.IsConfigured)
        {
            throw new InvalidOperationException(
                "Google Drive backup isn't configured yet. Set the client id for this platform " +
                "(see IGoogleDriveOAuthPlatform), or provide an override at " +
                $"\"{AppPaths.GoogleDriveOAuthConfigFilePath}\".");
        }

        SyncProgress.Report(progress, "Opening the browser to sign in to Google...");

        // Desktop clients supply a client secret (Google requires it for "Desktop app" clients);
        // mobile clients have none and rely on PKCE. The platform's code receiver captures the
        // redirect.
        UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            CreateClientSecrets(oauthOptions),
            Scopes,
            "user",
            cancellationToken,
            new FileDataStore(AppPaths.GoogleDriveTokenFolder, fullPath: true),
            _platform.CreateCodeReceiver());

        SyncProgress.Report(progress, "Retrieving your account details...");

        using var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Broccoli",
        });

        AboutResource.GetRequest aboutRequest = driveService.About.Get();
        aboutRequest.Fields = "user";
        Google.Apis.Drive.v3.Data.About about = await aboutRequest.ExecuteAsync(cancellationToken);

        var account = new GoogleDriveAccountInfo
        {
            Email = about.User.EmailAddress,
            ConnectedAtUtc = DateTime.UtcNow,
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

        GoogleDriveOAuthOptions oauthOptions = LoadOAuthOptions();
        if (!oauthOptions.IsConfigured)
        {
            return null;
        }

        try
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                CreateClientSecrets(oauthOptions),
                Scopes,
                "user",
                cancellationToken,
                new FileDataStore(AppPaths.GoogleDriveTokenFolder, fullPath: true),
                _platform.CreateCodeReceiver());

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Broccoli",
            });
        }
        catch (Exception)
        {
            // Cached token missing/revoked/expired beyond refresh — treat as not connected.
            // The user will see "Not connected" in Settings and can reconnect explicitly.
            return null;
        }
    }

    private ClientSecrets CreateClientSecrets(GoogleDriveOAuthOptions oauthOptions)
    {
        var clientSecrets = new ClientSecrets { ClientId = oauthOptions.ClientId };
        if (_platform.ClientSecret is not null)
        {
            clientSecrets.ClientSecret = _platform.ClientSecret;
        }

        return clientSecrets;
    }

    private GoogleDriveOAuthOptions LoadOAuthOptions()
    {
        string path = AppPaths.GoogleDriveOAuthConfigFilePath;

        // A user-supplied override (optional) takes precedence over the platform-embedded id.
        if (File.Exists(path))
        {
            try
            {
                GoogleDriveOAuthOptions? options = JsonSerializer.Deserialize<GoogleDriveOAuthOptions>(File.ReadAllText(path));
                if (options is not null && !string.IsNullOrWhiteSpace(options.ClientId))
                {
                    return options;
                }
            }
            catch (JsonException)
            {
                // Corrupt/partial override file — fall back to the platform-embedded id.
            }
        }

        return new GoogleDriveOAuthOptions { ClientId = _platform.ClientId };
    }
}
