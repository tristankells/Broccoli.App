using Broccoli.App.Shared.Models;

namespace Broccoli.App.Shared.Services;

public interface IAuthenticationService
{
    Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password);
    Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string password);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task UpdateLastLoginAsync(string userId);
}
