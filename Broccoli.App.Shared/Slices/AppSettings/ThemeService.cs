using Broccoli.App.Shared.Platform;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Slices.AppSettings;

public class ThemeService : IThemeService
{
    private const string ThemeKey = "AppTheme";

    private readonly ISecureStorageService _secureStorage;
    private readonly ILogger<ThemeService> _logger;

    public event Action? OnThemeChanged;
    public bool IsDarkMode { get; private set; }

    public ThemeService(ISecureStorageService secureStorage, ILogger<ThemeService> logger)
    {
        _secureStorage = secureStorage;
        _logger = logger;
    }

    public async Task InitializeAsync(bool osPrefersDark = false)
    {
        try
        {
            var saved = await _secureStorage.GetAsync(ThemeKey);
            if (string.IsNullOrEmpty(saved))
            {
                // No saved preference — fall back to OS preference (no write yet)
                IsDarkMode = osPrefersDark;
            }
            else
            {
                IsDarkMode = bool.TryParse(saved, out var parsed) && parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load theme preference; defaulting to OS preference.");
            IsDarkMode = osPrefersDark;
        }

        OnThemeChanged?.Invoke();
    }

    public async Task SetThemeAsync(bool isDark)
    {
        IsDarkMode = isDark;

        try
        {
            await _secureStorage.SetAsync(ThemeKey, isDark.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist theme preference.");
        }

        OnThemeChanged?.Invoke();
    }
}

