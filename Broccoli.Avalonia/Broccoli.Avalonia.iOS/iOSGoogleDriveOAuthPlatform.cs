using Broccoli.Avalonia.Slices.Settings;
using Google.Apis.Auth.OAuth2;

namespace Broccoli.Avalonia.IOS;

/// <summary>
/// iOS implementation: an embedded "iOS" client id plus a custom URI-scheme code receiver using
/// Google's reverse client-id redirect (<c>com.googleusercontent.apps.{clientId}:/oauth2redirect</c>).
/// The reverse client-id scheme is allowed automatically for an "iOS" OAuth client — no manual
/// redirect URI registration needed. The scheme must also be declared in <c>Info.plist</c> under
/// <c>CFBundleURLTypes</c>.
/// </summary>
public sealed class IosGoogleDriveOAuthPlatform : IGoogleDriveOAuthPlatform
{
    /// <summary>The redirect path Google appends after the reverse client-id scheme.</summary>
    public const string RedirectPath = "/oauth2redirect";

    /// <summary>
    /// The custom URI scheme for the OAuth redirect — Google's reverse client-id form. Declared in
    /// <c>Info.plist</c> under <c>CFBundleURLTypes</c>.
    /// </summary>
    public const string Scheme = "com.googleusercontent.apps." + ClientIdValue;

    /// <summary>
    /// The OAuth client id for the "iOS" client registered in
    /// <see href="https://console.cloud.google.com/apis/credentials">Google Cloud Console</see>.
    /// Public value — safe to ship. Paste the iOS client id here once ready. Keep the matching
    /// scheme in Info.plist (CFBundleURLTypes) in sync.
    /// </summary>
    private const string ClientIdValue = "";

    public string ClientId => ClientIdValue;

    public string? ClientSecret => null;

    public ICodeReceiver CreateCodeReceiver() =>
        new MobileSchemeCodeReceiver($"{Scheme}:{RedirectPath}", OpenBrowser);

    private static void OpenBrowser(Uri authorizationUri)
    {
        global::UIKit.UIApplication.SharedApplication.OpenUrl(
            new global::Foundation.NSUrl(authorizationUri.ToString()),
            new global::Foundation.NSDictionary(),
            static success => { });
    }
}
