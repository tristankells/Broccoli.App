using System.Diagnostics;

namespace Broccoli.Avalonia.Desktop.E2ETests;

/// <summary>
/// Ensures an Appium server (with the <c>windows</c> driver) is reachable on
/// <c>127.0.0.1:4723</c>, starting one on demand if necessary. The server is process-wide and
/// shared by all tests; it is only shut down here if this class started it (a server the user
/// started themselves is left alone).
/// </summary>
public static class AppiumServer
{
    public const string BaseUrl = "http://127.0.0.1:4723";

    private static readonly object SyncRoot = new();

    private static Process? _startedProcess;

    /// <summary>
    /// Returns the Appium base URL, ensuring a server is running. Requires Appium 2.x with the
    /// <c>windows</c> driver installed (the driver manages WinAppDriver itself).
    /// </summary>
    public static string EnsureRunning(TestContext context)
    {
        lock (SyncRoot)
        {
            VerifyDriverPatched();

            if (IsReachable())
            {
                context.WriteLine($"Appium server already running at {BaseUrl}.");
                return BaseUrl;
            }

            if (_startedProcess is not null && !_startedProcess.HasExited)
            {
                WaitUntilReachable(context, TimeSpan.FromSeconds(90), _startedProcess);
                return BaseUrl;
            }

            string? appiumBin = ResolveAppiumBin();
            if (appiumBin is null)
            {
                throw new InvalidOperationException(
                    "Appium could not be found. Install it with 'npm install -g appium' followed by " +
                    "'appium driver install windows', or start a server yourself on port 4723.");
            }

            string logFile = Path.Combine(Path.GetTempPath(), "broccoli-e2e-appium.log");
            string serverArgs =
                $"--address 127.0.0.1 --port 4723 --log \"{logFile}\" --log-level debug";

            context.WriteLine($"Starting Appium server from '{appiumBin}'.");
            Process process = LaunchProcess(appiumBin, serverArgs);
            _startedProcess = process;

            WaitUntilReachable(context, TimeSpan.FromSeconds(90), process);
            return BaseUrl;
        }
    }

    /// <summary>Shuts down the Appium server if this class started it (called from assembly cleanup).</summary>
    public static void Shutdown()
    {
        lock (SyncRoot)
        {
            if (_startedProcess is not null && !_startedProcess.HasExited)
            {
                KillTree(_startedProcess);
            }

            _startedProcess = null;
        }
    }

    /// <summary>
    /// The Appium windows driver (2.x) needs a small patch to work with classic WinAppDriver
    /// (appium/appium-windows-driver#316). Fail fast with clear instructions if it is missing.
    /// </summary>
    private static void VerifyDriverPatched()
    {
        string driverFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".appium", "node_modules", "appium-windows-driver", "build", "lib", "winappdriver.js");
        if (!File.Exists(driverFile))
        {
            return; // Let the Appium server surface its own "driver not installed" error.
        }

        if (!File.ReadAllText(driverFile).Contains("// [broccoli-e2e-patch]"))
        {
            throw new InvalidOperationException(
                "The Appium windows driver has not been patched for classic WinAppDriver. " +
                "Run the one-time setup:  node Broccoli.Avalonia.Desktop.E2ETests/patch-windows-driver.js");
        }
    }

    private static void WaitUntilReachable(TestContext context, TimeSpan timeout, Process process)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (IsReachable())
            {
                context.WriteLine("Appium server is ready.");
                return;
            }

            if (process.HasExited)
            {
                break;
            }

            Thread.Sleep(500);
        }

        string logFile = Path.Combine(Path.GetTempPath(), "broccoli-e2e-appium.log");
        throw new TimeoutException(
            "Appium server did not become reachable. Check its log: " + logFile);
    }

    private static bool IsReachable()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using HttpResponseMessage response = http
                .GetAsync(BaseUrl + "/status")
                .GetAwaiter()
                .GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? ResolveAppiumBin()
    {
        string? configured = Environment.GetEnvironmentVariable("APPIUM_BIN");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return configured;
        }

        string[] pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in pathEntries)
        {
            foreach (string name in new[] { "appium.cmd", "appium.bat", "appium.exe", "appium" })
            {
                string candidate = Path.Combine(entry, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static Process LaunchProcess(string appiumBin, string serverArgs)
    {
        bool isBatch = appiumBin.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
            || appiumBin.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

        ProcessStartInfo startInfo = new()
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        if (isBatch)
        {
            // .cmd/.bat shims cannot be launched directly with UseShellExecute=false.
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c \"\"{appiumBin}\" {serverArgs}\"";
        }
        else
        {
            startInfo.FileName = appiumBin;
            startInfo.Arguments = serverArgs;
        }

        return Process.Start(startInfo)!;
    }

    private static void KillTree(Process process)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = $"/PID {process.Id} /T /F",
                UseShellExecute = false,
                CreateNoWindow = true,
            })?.WaitForExit(5000);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
