using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.iOS;
using Broccoli.Avalonia.Slices.Settings;
using Foundation;
using UIKit;

namespace Broccoli.Avalonia.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    public AppDelegate()
    {
        // Avalonia routes custom URL schemes through the Activated event (ProtocolActivatedEventArgs).
        ((IAvaloniaAppDelegate)this).Activated += OnActivated;
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Supply the iOS-specific Google OAuth client id/code receiver before Avalonia builds the
        // DI container (CustomizeAppBuilder runs before SetupWithLifetime, which triggers
        // App.OnFrameworkInitializationCompleted).
        App.GoogleDriveOAuthPlatformOverride = new iOSGoogleDriveOAuthPlatform();

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    private static void OnActivated(object? sender, ActivatedEventArgs eventArgs)
    {
        // The OAuth redirect arrives as a deep link on the reverse client-id scheme; forward it to
        // the code receiver that the auth flow is waiting on.
        if (eventArgs is ProtocolActivatedEventArgs protocol &&
            protocol.Uri.Scheme == iOSGoogleDriveOAuthPlatform.Scheme)
        {
            MobileSchemeCodeReceiver.HandleRedirectUri(protocol.Uri);
        }
    }
}
