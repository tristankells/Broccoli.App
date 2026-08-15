using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Broccoli.Avalonia.Android;

[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        // Supply the Android-specific Google Drive auth service before Avalonia builds the DI
        // container (which happens inside base.OnCreate via App.OnFrameworkInitializationCompleted).
        App.GoogleDriveAuthServiceOverride = new AndroidGoogleDriveAuthService();

        base.OnCreate();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
        .WithInterFont();
    }
}
