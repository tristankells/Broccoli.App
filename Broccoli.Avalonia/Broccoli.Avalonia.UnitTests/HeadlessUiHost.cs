using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Broccoli.Avalonia.Tests;

/// <summary>
/// Minimal harness for running real Avalonia views headlessly in MSTest. Starts a single
/// <see cref="HeadlessUnitTestSession"/> lazily and reuses it for the whole test run, so each
/// UI test is just <see cref="Run(Func{Control}, Action{Window})"/> plus assertions.
/// <para>
/// Input is driven through keyboard helpers (<c>Focus()</c> + <c>KeyTextInput</c> /
/// <c>KeyPress</c>), which is the supported headless pattern; mouse hit-testing is not
/// functional in the current headless platform, so coordinate clicks are avoided.
/// </para>
/// </summary>
public static class HeadlessUiHost
{
    private static HeadlessUnitTestSession? _session;

    private static HeadlessUnitTestSession Session =>
        _session ??= HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));

    /// <summary>
    /// Shows a view in a headless window on the session's UI thread and runs <paramref name="test"/>
    /// against it. <paramref name="contentFactory"/> runs on the UI thread (Avalonia controls have
    /// thread affinity and must be created there) and is attached before <c>Show</c> so the view is
    /// measured and arranged. Assertions thrown inside are surfaced to the test runner.
    /// </summary>
    public static void Run(Func<Control> contentFactory, Action<Window> test)
    {
        Session.Dispatch(
            () => test(ShowWindow(contentFactory())),
            CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Creates and shows a headless window, flushing layout so controls are measurable.</summary>
    public static Window ShowWindow(Control? content = null, int width = 800, int height = 600)
    {
        var window = new Window { Width = width, Height = height };
        if (content is not null)
        {
            window.Content = content;
        }

        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>Returns every <typeparamref name="T"/> control in <paramref name="root"/>'s visual tree.</summary>
    public static IReadOnlyList<T> FindVisualChildren<T>(Visual root) where T : Control
        => root.GetVisualDescendants().OfType<T>().ToList();
}
