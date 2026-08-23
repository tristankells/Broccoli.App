using Avalonia;
using Avalonia.Headless;

namespace Broccoli.Avalonia.Tests;

/// <summary>
/// Entry point for the headless test session, consumed by <see cref="HeadlessUiHost"/>.
/// Follows the pattern from the official Avalonia headless-testing docs: a dedicated builder
/// that installs the headless platform against the lightweight <see cref="TestApplication"/>.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
