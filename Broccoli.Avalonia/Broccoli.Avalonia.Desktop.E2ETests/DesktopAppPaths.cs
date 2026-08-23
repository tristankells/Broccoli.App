namespace Broccoli.Avalonia.Desktop.E2ETests;

/// <summary>
/// Locates the built desktop app executable that the E2E tests drive. The desktop project is a
/// build dependency of this test project, so running the tests builds the app first; the exe is
/// then resolved from the repo layout relative to the test's own output folder.
/// </summary>
public static class DesktopAppPaths
{
    private const string RelativeExePath = "Broccoli.Avalonia\\Broccoli.Avalonia.Desktop\\bin\\{0}\\net10.0\\Broccoli.Avalonia.Desktop.exe";

    /// <summary>Full path to <c>Broccoli.Avalonia.Desktop.exe</c>.</summary>
    public static string DesktopExe
    {
        get
        {
            string? fromEnvironment = Environment.GetEnvironmentVariable("BROCCOLI_DESKTOP_EXE");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return Path.GetFullPath(fromEnvironment);
            }

            string configuration =
#if DEBUG
                "Debug";
#else
                "Release";
#endif

            string relative = string.Format(
                System.Globalization.CultureInfo.InvariantCulture, RelativeExePath, configuration);

            DirectoryInfo? current = new(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && current is not null; i++, current = current.Parent)
            {
                string candidate = Path.Combine(current.FullName, relative);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            throw new FileNotFoundException(
                "Could not locate the built desktop app. Build 'Broccoli.Avalonia.Desktop' first, " +
                "or set the BROCCOLI_DESKTOP_EXE environment variable to the path of the exe.");
        }
    }
}
