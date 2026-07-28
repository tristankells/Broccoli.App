namespace Broccoli.Avalonia.Services;

/// <summary>
/// OAuth "installed application" client credentials for the Google Drive backup login flow.
/// Not baked into the app binary — read from a user-editable local JSON file so anyone who
/// wants to enable Drive backup can supply their own OAuth client registered in
/// <see href="https://console.cloud.google.com/apis/credentials">Google Cloud Console</see>
/// (type "Desktop app"), without requiring a rebuild.
/// </summary>
public class GoogleDriveOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
