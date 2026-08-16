using System.Text.Json;
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.Identity;
using Android.Gms.Common.Apis;
using Android.Gms.Extensions;
using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using Broccoli.Avalonia.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace Broccoli.Avalonia.Android;

/// <summary>
/// Android implementation of the Google Drive auth flow, built on Google Identity Services'
/// <c>AuthorizationClient</c> (Google Play services) rather than the shared loopback/custom-scheme
/// OAuth flow. Google has deprecated custom URI schemes and loopback redirects for Android, so this
/// is the supported approach: the client uses the device's Google account, identifies the app via
/// its package name + signing certificate (SHA-1), and returns an access token directly — no client
/// secret, client id, or redirect URI is needed in code.
///
/// The first connection prompts for consent (via a PendingIntent resolved in
/// <see cref="MainActivity"/>'s <c>OnActivityResult</c>); once granted, subsequent authorizations
/// return a fresh access token silently, so background sync never re-prompts.
/// </summary>
public sealed class AndroidGoogleDriveAuthService : IGoogleDriveAuthService
{
    private const int ConsentRequestCode = 4310;
    private const string DriveFileScope = "https://www.googleapis.com/auth/drive.file";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Completes the interactive consent flow started in ConnectAsync.
    private static TaskCompletionSource<AuthorizationResult>? _pendingConsent;

    /// <summary>Set by <see cref="MainActivity"/> so the interactive consent flow has an activity to launch from.</summary>
    public static Activity? CurrentActivity { get; set; }

    /// <summary>
    /// Called by <see cref="MainActivity.OnActivityResult"/> to deliver the consent result back to
    /// whichever <see cref="ConnectAsync"/> is awaiting it.
    /// </summary>
    public static void HandleActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != ConsentRequestCode || _pendingConsent is null)
        {
            return;
        }

        if (data is null)
        {
            _pendingConsent.TrySetCanceled();
            _pendingConsent = null;
            return;
        }

        IAuthorizationClient client = GetClient();
        AuthorizationResult result = client.GetAuthorizationResultFromIntent(data);
        _pendingConsent.TrySetResult(result);
        _pendingConsent = null;
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
            return null;
        }
    }

    public async Task<GoogleDriveAccountInfo> ConnectAsync(IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        SyncProgress.Report(progress, "Requesting Drive access from Google...");

        AuthorizationResult result = await AuthorizeAsync(cancellationToken);

        if (result.HasResolution)
        {
            SyncProgress.Report(progress, "Waiting for you to approve access in the browser...");
            result = await LaunchConsentAsync(result, cancellationToken);
        }

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            throw new InvalidOperationException("Google Drive authorization was not completed.");
        }

        SyncProgress.Report(progress, "Retrieving your account details...");

        using DriveService drive = CreateDriveService(result.AccessToken);
        string email = await GetEmailAsync(drive, cancellationToken);

        var account = new GoogleDriveAccountInfo
        {
            Email = email,
            ConnectedAtUtc = DateTime.UtcNow,
        };

        File.WriteAllText(AppPaths.GoogleDriveAccountFilePath, JsonSerializer.Serialize(account, JsonOptions));
        return account;
    }

    public async Task<DriveService?> TryGetDriveServiceAsync(CancellationToken cancellationToken = default)
    {
        if (GetStoredAccount() is null)
        {
            return null;
        }

        try
        {
            AuthorizationResult result = await AuthorizeAsync(cancellationToken);

            // Not yet granted (would need a consent prompt) or no token — treat as not connected.
            if (result.HasResolution || string.IsNullOrEmpty(result.AccessToken))
            {
                return null;
            }

            return CreateDriveService(result.AccessToken);
        }
        catch (Exception)
        {
            return null;
        }
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

    private static async Task<AuthorizationResult> AuthorizeAsync(CancellationToken cancellationToken)
    {
        var builder = new AuthorizationRequest.Builder();
        builder.SetRequestedScopes(new List<Scope> { new Scope(DriveFileScope) });
        AuthorizationRequest request = builder.Build();

        IAuthorizationClient client = GetClient();
        return await client.Authorize(request).AsAsync<AuthorizationResult>();
    }

    private static Task<AuthorizationResult> LaunchConsentAsync(AuthorizationResult result, CancellationToken cancellationToken)
    {
        Activity? activity = CurrentActivity;
        if (activity is null)
        {
            throw new InvalidOperationException("No activity is available to show the Google consent screen.");
        }

        _pendingConsent = new TaskCompletionSource<AuthorizationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() =>
        {
            _pendingConsent?.TrySetCanceled(cancellationToken);
            _pendingConsent = null;
        });

        activity.StartIntentSenderForResult(
            result.PendingIntent!.IntentSender,
            ConsentRequestCode,
            null,
            0,
            0,
            0);

        return _pendingConsent.Task;
    }

    private static IAuthorizationClient GetClient()
    {
        Context context = CurrentActivity ?? Application.Context;
        return Identity.GetAuthorizationClient(context);
    }

    private static DriveService CreateDriveService(string accessToken)
    {
        GoogleCredential credential = GoogleCredential.FromAccessToken(accessToken);
        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Broccoli",
        });
    }

    private static async Task<string> GetEmailAsync(DriveService drive, CancellationToken cancellationToken)
    {
        AboutResource.GetRequest aboutRequest = drive.About.Get();
        aboutRequest.Fields = "user";
        Google.Apis.Drive.v3.Data.About about = await aboutRequest.ExecuteAsync(cancellationToken);
        return about.User.EmailAddress;
    }
}
