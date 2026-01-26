using Broccoli.App.Shared.Models;

namespace Broccoli.App.Shared.Services;

public interface IAuthenticationStateService
{
    event Action? OnAuthenticationStateChanged;
    
    AuthenticationState CurrentState { get; }
    
    Task LoginAsync(User user);
    Task LogoutAsync();
    Task InitializeAsync();
    bool IsAuthenticated { get; }
    string? CurrentUsername { get; }
    string? CurrentUserId { get; }
}
