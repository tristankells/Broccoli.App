using Broccoli.Data.Models;

namespace Broccoli.App.Shared._Shared.Infrastructure;

public interface IUserService
{
    Task InitializeAsync();
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
}
