using Broccoli.App.Shared.Models;

namespace Broccoli.App.Shared.Infrastructure;

public interface ICosmosDbService
{
    Task InitializeAsync();
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
}
