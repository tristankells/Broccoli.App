using Avalonia;
using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Desktop;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        ApplyAppDataOverride(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Reads an optional <c>--appdata &lt;folder&gt;</c> argument so the app can be pointed at an
    /// alternative data folder (used by end-to-end tests to avoid touching real user data).
    /// </summary>
    private static void ApplyAppDataOverride(string[] args)
    {
        const string flag = "--appdata";
        int index = Array.IndexOf(args, flag);
        if (index >= 0 && index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1]))
        {
            AppPaths.OverrideRootFolder(Path.GetFullPath(args[index + 1]));
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
