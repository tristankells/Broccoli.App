namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// OAuth "installed application" client id for the Google Drive backup login flow.
/// Only the client id is needed — the flow uses PKCE (RFC 7636) rather than a client secret,
/// because an installed/desktop app cannot keep a secret confidential. The id is public by
/// design and ships embedded per-platform (see <see cref="IGoogleDriveOAuthPlatform"/>), but a
/// user can optionally override it via a local JSON file without a rebuild.
/// </summary>
public class GoogleDriveOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);
}
