namespace Broccoli.App.Shared.Platform;

public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}
