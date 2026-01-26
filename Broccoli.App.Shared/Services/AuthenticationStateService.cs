using System.Text.Json;
using Broccoli.App.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Services;

public class AuthenticationStateService : IAuthenticationStateService
{
    private readonly ISecureStorageService _secureStorage;
    private readonly ILogger<AuthenticationStateService> _logger;
    private const string AuthStateKey = "AuthenticationState";

    public event Action? OnAuthenticationStateChanged;

    public AuthenticationState CurrentState { get; private set; } = new();

    public bool IsAuthenticated => CurrentState.IsAuthenticated;
    public string? CurrentUsername => CurrentState.Username;
    public string? CurrentUserId => CurrentState.UserId;

    public AuthenticationStateService(
        ISecureStorageService secureStorage,
        ILogger<AuthenticationStateService> logger)
    {
        _secureStorage = secureStorage;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var storedState = await _secureStorage.GetAsync(AuthStateKey);
            if (!string.IsNullOrWhiteSpace(storedState))
            {
                var state = JsonSerializer.Deserialize<AuthenticationState>(storedState);
                if (state != null)
                {
                    CurrentState = state;
                    _logger.LogInformation("Restored authentication state for user {Username}", state.Username);
                    NotifyAuthenticationStateChanged();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing authentication state");
        }
    }

    public async Task LoginAsync(User user)
    {
        CurrentState = new AuthenticationState
        {
            IsAuthenticated = true,
            Username = user.Username,
            UserId = user.Id,
            LoginTime = DateTime.UtcNow
        };

        await PersistStateAsync();
        NotifyAuthenticationStateChanged();
        
        _logger.LogInformation("User {Username} logged in", user.Username);
    }

    public async Task LogoutAsync()
    {
        CurrentState = new AuthenticationState();
        await _secureStorage.RemoveAsync(AuthStateKey);
        NotifyAuthenticationStateChanged();
        
        _logger.LogInformation("User logged out");
    }

    private async Task PersistStateAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(CurrentState);
            await _secureStorage.SetAsync(AuthStateKey, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting authentication state");
        }
    }

    private void NotifyAuthenticationStateChanged()
    {
        OnAuthenticationStateChanged?.Invoke();
    }
}
