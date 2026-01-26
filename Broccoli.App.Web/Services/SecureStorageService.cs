using Broccoli.App.Shared.Services;
using System.Collections.Concurrent;

namespace Broccoli.App.Web.Services;

public class SecureStorageService : ISecureStorageService
{
    // Use a static dictionary to persist across scoped instances within the same process
    // In production, you'd want to use a proper session store or database
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _circuitStorage = new();
    private readonly string _circuitId;

    public SecureStorageService()
    {
        // Generate a unique circuit ID for this service instance
        // In a real production app, you'd use the actual Blazor circuit ID
        _circuitId = Guid.NewGuid().ToString();
        _circuitStorage.TryAdd(_circuitId, new ConcurrentDictionary<string, string>());
    }

    public Task<string?> GetAsync(string key)
    {
        // Try to get from any circuit (shared authentication state)
        foreach (var circuit in _circuitStorage.Values)
        {
            if (circuit.TryGetValue(key, out var value))
            {
                return Task.FromResult<string?>(value);
            }
        }
        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(string key, string value)
    {
        // Store in all circuits to ensure persistence
        foreach (var circuit in _circuitStorage.Values)
        {
            circuit[key] = value;
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        // Remove from all circuits
        foreach (var circuit in _circuitStorage.Values)
        {
            circuit.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }
}
