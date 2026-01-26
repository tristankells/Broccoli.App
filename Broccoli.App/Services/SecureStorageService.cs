using Broccoli.App.Shared.Services;

namespace Broccoli.App.Services;

public class SecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        await SecureStorage.SetAsync(key, value);
    }

    public Task RemoveAsync(string key)
    {
        SecureStorage.Remove(key);
        return Task.CompletedTask;
    }
}
