namespace Broccoli.App.Shared.Slices.AppSettings;

public interface IThemeService
{
    event Action? OnThemeChanged;
    bool IsDarkMode { get; }
    Task InitializeAsync(bool osPrefersDark = false);
    Task SetThemeAsync(bool isDark);
}

