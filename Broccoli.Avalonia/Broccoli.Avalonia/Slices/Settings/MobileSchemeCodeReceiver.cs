using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;

namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// Custom URI-scheme OAuth code receiver for mobile (Android/iOS).
///
/// Google.Apis.Auth's <see cref="LocalServerCodeReceiver"/> (the default used by
/// <see cref="GoogleWebAuthorizationBroker"/>) spins up a loopback HTTP listener, which only
/// works on Desktop. On mobile the OAuth flow redirects to a custom URI scheme and the OS
/// deep-links back into the app, which then forwards the query string here.
///
/// This receiver is configured with the full redirect URI — for a Google "Android"/"iOS" client
/// that is the reverse client-id URI <c>com.googleusercontent.apps.{clientId}:/oauth2redirect</c>,
/// which Google allows automatically (no manual redirect registration) and ties to the app's
/// package + signing fingerprint.
///
/// Platform wiring required:
///   - Android (AndroidManifest.xml / [IntentFilter]): register the reverse client-id scheme and
///     path, then forward <c>Intent.Data</c> from <c>OnCreate</c>/<c>OnNewIntent</c> to
///     <see cref="HandleRedirectUri"/>.
///   - iOS (Info.plist): register the scheme under <c>CFBundleURLTypes</c>, then forward the URL
///     from <c>OpenUrl</c>/<c>ContinueUserActivity</c> in <c>AppDelegate</c> to
///     <see cref="HandleRedirectUri"/>.
/// </summary>
public sealed class MobileSchemeCodeReceiver : ICodeReceiver
{
    private readonly string _redirectUri;
    private readonly Action<Uri> _openBrowser;

    // One in-flight login at a time; completed by the platform head via HandleRedirectUri.
    private static TaskCompletionSource<AuthorizationCodeResponseUrl>? _pendingLogin;

    // A redirect can arrive before ReceiveCodeAsync runs (e.g. the OS cold-started the app to
    // deliver the deep link). Stash it here and replay it once the flow asks for the code.
    private static AuthorizationCodeResponseUrl? _earlyRedirect;

    /// <param name="redirectUri">The full redirect URI registered for the OAuth client.</param>
    /// <param name="openBrowser">Opens the system browser for the given authorization URI.</param>
    public MobileSchemeCodeReceiver(string redirectUri, Action<Uri> openBrowser)
    {
        _redirectUri = redirectUri;
        _openBrowser = openBrowser;
    }

    /// <inheritdoc />
    public string RedirectUri => _redirectUri;

    /// <inheritdoc />
    public Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
        AuthorizationCodeRequestUrl url, CancellationToken cancellationToken)
    {
        _pendingLogin = new TaskCompletionSource<AuthorizationCodeResponseUrl>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // If the OS already routed a redirect to us before this flow started, complete now.
        if (_earlyRedirect is not null)
        {
            AuthorizationCodeResponseUrl early = _earlyRedirect;
            _earlyRedirect = null;
            _pendingLogin.TrySetResult(early);
            return _pendingLogin.Task;
        }

        _openBrowser(url.Build());

        return _pendingLogin.Task;
    }

    /// <summary>
    /// Called by the platform head when the OS routes the deep link back into the app, e.g.
    /// <c>com.googleusercontent.apps.{clientId}:/oauth2redirect?code=...&amp;state=...</c>.
    /// </summary>
    public static void HandleRedirectUri(Uri redirectUri)
    {
        AuthorizationCodeResponseUrl response =
            new AuthorizationCodeResponseUrl(redirectUri.Query.TrimStart('?'));

        if (_pendingLogin is null)
        {
            // Arrived before ReceiveCodeAsync — keep it for when the flow starts.
            _earlyRedirect = response;
            return;
        }

        _pendingLogin.TrySetResult(response);
        _pendingLogin = null;
    }
}
