using System.Security.Cryptography;
using System.Text;
using Broccoli.App.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(ICosmosDbService cosmosDbService, ILogger<AuthenticationService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Username and password are required.", null);
            }

            var user = await GetUserByUsernameAsync(username);
            
            if (user == null)
            {
                return (false, "UserNotFound", null);
            }

            if (!await ValidatePasswordAsync(user, password))
            {
                return (false, "Incorrect password.", null);
            }

            await UpdateLastLoginAsync(user.Id);
            user.LastLoginAt = DateTime.UtcNow;

            return (true, "Login successful.", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", username);
            return (false, "An error occurred during login.", null);
        }
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Username and password are required.", null);
            }

            if (password.Length < 6)
            {
                return (false, "Password must be at least 6 characters.", null);
            }

            var existingUser = await GetUserByUsernameAsync(username);
            if (existingUser != null)
            {
                return (false, "Username already exists.", null);
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                PartitionKey = "user"
            };

            var createdUser = await _cosmosDbService.CreateUserAsync(user);
            
            return (true, "Registration successful.", createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Username}", username);
            return (false, "An error occurred during registration.", null);
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _cosmosDbService.GetUserByUsernameAsync(username);
    }

    public Task<bool> ValidatePasswordAsync(User user, string password)
    {
        var hashedPassword = HashPassword(password);
        return Task.FromResult(user.PasswordHash == hashedPassword);
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        try
        {
            var user = await _cosmosDbService.GetUserByUsernameAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _cosmosDbService.UpdateUserAsync(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user {UserId}", userId);
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
