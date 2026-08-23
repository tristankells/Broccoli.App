namespace Broccoli.Avalonia.Desktop.E2ETests;

/// <summary>
/// Process-wide hooks for the E2E test run.
/// </summary>
[TestClass]
public static class E2ETestHooks
{
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        AppiumServer.Shutdown();
    }
}
