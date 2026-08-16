using Google.Apis.Auth.OAuth2;

namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// Desktop default: an embedded "Desktop app" client id + secret plus a loopback HTTP code
/// receiver. Google's "Desktop app" clients still require a client secret in the token exchange —
/// it ships in the binary and is not treated as confidential (PKCE is used in addition).
/// </summary>
public sealed class DesktopGoogleDriveOAuthPlatform : IGoogleDriveOAuthPlatform
{
    /// <summary>
    /// The embedded OAuth client id for the "Desktop app" client registered in
    /// <see href="https://console.cloud.google.com/apis/credentials">Google Cloud Console</see>.
    /// Public value — safe to ship. Paste the desktop client id here once and Drive backup works
    /// out of the box on every desktop install. Ensure the client's redirect URI includes the
    /// loopback <c>http://127.0.0.1/authorize/</c>.
    /// </summary>
    private const string ClientIdValue = "129355131159-qrbe4dc92p3p3oci0k8bvu8vs0q92gmt.apps.googleusercontent.com";

    /// <summary>
    /// The client secret for the same "Desktop app" client (shown alongside the client id in the
    /// Cloud Console). Google requires it for the desktop token exchange, but it is not treated as
    /// confidential for installed apps, so it ships embedded in the binary.
    /// </summary>
    private const string ClientSecretValue = "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";

    public string ClientId => ClientIdValue;

    public string ClientSecret => ClientSecretValue;

    public ICodeReceiver CreateCodeReceiver() => new LocalServerCodeReceiver();
}
