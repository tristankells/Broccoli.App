using Broccoli.Data.Models;
using Broccoli.App.Shared.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Broccoli.Shared.Services;

/// <summary>
/// CosmosDB implementation of IPantryService.
/// Stores pantry items in the "PantryItems" container, partitioned by "partitionKey" = "user".
/// User-level filtering is applied via the userId field in all queries.
/// </summary>
public class PantryService : IPantryService
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<PantryService> _logger;
    private Container? _container;
    private bool _initialized;

    private const string DatabaseId = "BroccoliAppDb";
    private const string ContainerId = "PantryItems";
    private const string PartitionKeyValue = "user";

    public PantryService(
        CosmosClient cosmosClient,
        ILogger<PantryService> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Initializing PantryItems container in CosmosDB...");

            var database = _cosmosClient.GetDatabase(DatabaseId);

            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = ContainerId,
                    PartitionKeyPath = "/partitionKey"
                });

            _container = containerResponse.Container;
            _initialized = true;

            _logger.LogInformation("PantryItems container ready.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PantryItems container.");
            throw;
        }
    }

    private Container GetContainer()
    {
        if (!_initialized || _container == null)
        {
            throw new InvalidOperationException("PantryService is not initialized. Call InitializeAsync() first.");
        }

        return _container;
    }

    public async Task<List<PantryItem>> GetAllAsync(string userId)
    {
        var container = GetContainer();

        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt ASC")
                .WithParameter("@userId", userId);

            var results = new List<PantryItem>();
            using var iterator = container.GetItemQueryIterator<PantryItem>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(PartitionKeyValue) });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Retrieved {Count} pantry items for user {UserId}.", results.Count, userId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pantry items for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<PantryItem> AddAsync(PantryItem item)
    {
        var container = GetContainer();

        try
        {
            item.Id = Guid.NewGuid().ToString();
            item.PartitionKey = PartitionKeyValue;
            item.CreatedAt = DateTime.UtcNow;

            var response = await container.CreateItemAsync(
                item,
                new PartitionKey(PartitionKeyValue));

            _logger.LogInformation("Added pantry item '{Name}' for user {UserId}.", item.Name, item.UserId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding pantry item '{Name}' for user {UserId}.", item.Name, item.UserId);
            throw;
        }
    }

    public async Task<PantryItem> UpdateAsync(PantryItem item)
    {
        var container = GetContainer();

        try
        {
            item.PartitionKey = PartitionKeyValue;

            var response = await container.ReplaceItemAsync(
                item,
                item.Id,
                new PartitionKey(PartitionKeyValue));

            _logger.LogInformation("Updated pantry item '{Name}' for user {UserId}.", item.Name, item.UserId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pantry item {Id} for user {UserId}.", item.Id, item.UserId);
            throw;
        }
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var container = GetContainer();

        try
        {
            await container.DeleteItemAsync<PantryItem>(
                id,
                new PartitionKey(PartitionKeyValue));

            _logger.LogInformation("Deleted pantry item {Id} for user {UserId}.", id, userId);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Pantry item {Id} not found for user {UserId} during delete.", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pantry item {Id} for user {UserId}.", id, userId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string userId, string itemName)
    {
        var items = await GetAllAsync(userId);
        var nameLower = itemName.Trim().ToLowerInvariant();
        return items.Any(i => i.Name.ToLowerInvariant().Contains(nameLower)
                              || nameLower.Contains(i.Name.ToLowerInvariant()));
    }
}

