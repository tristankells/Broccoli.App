using Avalonia;
using Avalonia.Headless;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Broccoli.Avalonia.Tests;

/// <summary>
/// Bootstraps the Avalonia headless platform once per test run so tests that construct
/// views/view models (which parse <c>Geometry</c> and other platform-backed objects) can run
/// without a display. Uses a bare <see cref="Application"/> so the app's own composition-root
/// (DI container, database migration) is not triggered.
/// </summary>
[TestClass]
public static class AvaloniaTestBootstrap
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        _ = AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
    }
}
