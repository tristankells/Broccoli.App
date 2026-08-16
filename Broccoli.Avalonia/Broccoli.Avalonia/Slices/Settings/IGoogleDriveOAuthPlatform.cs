using Google.Apis.Auth.OAuth2;

namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// Platform-specific Google Drive OAuth configuration. The client id is public (not secret), so
/// it is embedded per-platform; the code receiver captures the OAuth redirect differently on each
/// platform — a loopback HTTP listener on desktop (<see cref="LocalServerCodeReceiver"/>) and a
/// custom URI scheme on mobile (<see cref="MobileSchemeCodeReceiver"/>).
/// </summary>
public interface IGoogleDriveOAuthPlatform
{
    /// <summary>The OAuth client id for this platform (public value, safe to ship).</summary>
    string ClientId { get; }

    /// <summary>
    /// The OAuth client secret, or null. Google's "Desktop app" clients require a client secret in
    /// the token exchange (the secret ships in the binary — Google does not treat it as
    /// confidential for installed apps). Android/iOS clients have no secret and return null.
    /// </summary>
    string? ClientSecret { get; }

    /// <summary>Creates the code receiver used to capture the OAuth authorization code redirect.</summary>
    ICodeReceiver CreateCodeReceiver();
}
