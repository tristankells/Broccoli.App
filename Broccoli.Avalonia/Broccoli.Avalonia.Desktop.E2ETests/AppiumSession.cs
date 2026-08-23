using System.Collections.ObjectModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace Broccoli.Avalonia.Desktop.E2ETests;

/// <summary>
/// One WinAppDriver session: launches the desktop app against a throwaway data folder and
/// exposes helpers to find elements and wait for UI state. Each session is a fresh app process,
/// so tests get clean state; dispose the session to close the app.
/// </summary>
public sealed class AppiumSession : IDisposable
{
    private readonly WindowsDriver _driver;
    private bool _disposed;

    private AppiumSession(WindowsDriver driver) => _driver = driver;

    /// <summary>
    /// Ensures the Appium server is running, then launches the desktop app with
    /// <c>--appdata &lt;appDataFolder&gt;</c> so it never touches real user data.
    /// </summary>
    public static AppiumSession Launch(
        TestContext context,
        string desktopExe,
        string appDataFolder,
        TimeSpan commandTimeout)
    {
        string baseUrl = AppiumServer.EnsureRunning(context);

        AppiumOptions options = new();
        options.PlatformName = "windows";
        options.AutomationName = "windows";
        // 'App' maps to 'appium:app'. The windows driver forwards capabilities verbatim to
        // WinAppDriver, which only understands un-prefixed names - the provided
        // patch-windows-driver.js makes the driver strip the 'appium:' prefix before forwarding.
        options.App = desktopExe;
        options.AddAdditionalAppiumOption("appium:appWorkingDir", Path.GetDirectoryName(desktopExe));
        options.AddAdditionalAppiumOption("appium:appArguments", $"--appdata \"{appDataFolder}\"");
        options.AddAdditionalAppiumOption("appium:newCommandTimeout", (int)commandTimeout.TotalSeconds);

        context.WriteLine($"Launching '{desktopExe}' with --appdata \"{appDataFolder}\".");
        WindowsDriver driver = new(new Uri(baseUrl), options, commandTimeout);
        return new AppiumSession(driver);
    }

    /// <summary>
    /// Polls for an element matching <paramref name="locator"/> until it appears or the timeout
    /// elapses. Throws <see cref="TimeoutException"/> on failure.
    /// </summary>
    public AppiumElement WaitForElement(By locator, TimeSpan? timeout = null)
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);
        DateTime deadline = DateTime.UtcNow.Add(effectiveTimeout);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                ReadOnlyCollection<AppiumElement> elements = _driver.FindElements(locator);
                if (elements.Count > 0)
                {
                    return elements[0];
                }
            }
            catch (WebDriverException exception)
            {
                lastError = exception;
            }

            Thread.Sleep(250);
        }

        throw new TimeoutException(
            $"Timed out after {effectiveTimeout.TotalSeconds:0}s waiting for {locator}.", lastError);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _driver.Quit();
        }
        catch
        {
            // The app may already be gone; nothing more to clean up.
        }
    }
}
