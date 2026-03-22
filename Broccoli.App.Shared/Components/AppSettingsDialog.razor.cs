using Microsoft.AspNetCore.Components;
using Broccoli.App.Shared.Services;

namespace Broccoli.App.Shared.Components;

public partial class AppSettingsDialog
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    [Inject] private IThemeService ThemeService { get; set; } = null!;
    [Inject] private IAuthenticationStateService AuthStateService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private async Task OnDarkModeToggled(ChangeEventArgs e)
    {
        var isDark = e.Value is bool b ? b : bool.TryParse(e.Value?.ToString(), out var parsed) && parsed;
        await ThemeService.SetThemeAsync(isDark);
    }

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private async Task Logout()
    {
        await OnClose.InvokeAsync();
        await AuthStateService.LogoutAsync();
        Navigation.NavigateTo("/login");
    }
}

