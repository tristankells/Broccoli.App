using Avalonia;
using Avalonia.Themes.Fluent;

namespace Broccoli.Avalonia.Tests;

/// <summary>
/// Lightweight Avalonia application used by UI tests. Deliberately NOT the app's real
/// <see cref="Broccoli.Avalonia.App"/>, so the composition root (DI container, EF database
/// migration) never runs in tests. Applies FluentTheme so controls get their default templates.
/// </summary>
public class TestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        base.Initialize();
    }
}
