using Broccoli.Data.Models;
using Broccoli.App.Shared.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Broccoli.Shared.Services;

/// <summary>
/// CosmosDB implementation of IGroceryListService.
/// Stores grocery list items in the "GroceryListItems" container, partitioned by "partitionKey" = "user".
/// User-level filtering is applied via the userId field in all queries.
/// </summary>
public class GroceryListService : IGroceryListService
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<GroceryListService> _logger;
    private Container? _container;
    private bool _initialized;

    private const string DatabaseId = "BroccoliAppDb";
    private const string ContainerId = "GroceryListItems";
    private const string PartitionKeyValue = "user";

    public GroceryListService(
        CosmosClient cosmosClient,
        ILogger<GroceryListService> logger)
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
            _logger.LogInformation("Initializing GroceryListItems container in CosmosDB...");

            var database = _cosmosClient.GetDatabase(DatabaseId);

            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = ContainerId,
                    PartitionKeyPath = "/partitionKey"
                });

            _container = containerResponse.Container;
            _initialized = true;

            _logger.LogInformation("GroceryListItems container ready.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing GroceryListItems container.");
            throw;
        }
    }

    private Container GetContainer()
    {
        if (!_initialized || _container == null)
        {
            throw new InvalidOperationException("GroceryListService is not initialized. Call InitializeAsync() first.");
        }

        return _container;
    }

    public async Task<List<GroceryListItem>> GetAllAsync(string userId)
    {
        var container = GetContainer();

        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt DESC")
                .WithParameter("@userId", userId);

            var results = new List<GroceryListItem>();
            using var iterator = container.GetItemQueryIterator<GroceryListItem>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(PartitionKeyValue) });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Retrieved {Count} grocery list items for user {UserId}.", results.Count, userId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grocery list items for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<GroceryListItem> AddAsync(GroceryListItem item)
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

            _logger.LogInformation("Added grocery list item '{Name}' for user {UserId}.", item.Name, item.UserId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding grocery list item '{Name}' for user {UserId}.", item.Name, item.UserId);
            throw;
        }
    }

    public async Task AddMultipleAsync(List<GroceryListItem> items)
    {
        var container = GetContainer();

        try
        {
            foreach (var item in items)
            {
                item.Id = Guid.NewGuid().ToString();
                item.PartitionKey = PartitionKeyValue;
                item.CreatedAt = DateTime.UtcNow;
            }

            var tasks = items
                .Select(item => container.CreateItemAsync(item, new PartitionKey(PartitionKeyValue)))
                .ToList();
            await Task.WhenAll(tasks);

            _logger.LogInformation("Added {Count} grocery list items for user.", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple grocery list items.");
            throw;
        }
    }

    public async Task<GroceryListItem> UpdateAsync(GroceryListItem item)
    {
        var container = GetContainer();

        try
        {
            item.PartitionKey = PartitionKeyValue;

            var response = await container.ReplaceItemAsync(
                item,
                item.Id,
                new PartitionKey(PartitionKeyValue));

            _logger.LogInformation("Updated grocery list item '{Name}' for user {UserId}.", item.Name, item.UserId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating grocery list item {Id} for user {UserId}.", item.Id, item.UserId);
            throw;
        }
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var container = GetContainer();

        try
        {
            await container.DeleteItemAsync<GroceryListItem>(
                id,
                new PartitionKey(PartitionKeyValue));

            _logger.LogInformation("Deleted grocery list item {Id} for user {UserId}.", id, userId);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Grocery list item {Id} not found for user {UserId} during delete.", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting grocery list item {Id} for user {UserId}.", id, userId);
            throw;
        }
    }

    public async Task ResetAsync(string userId)
    {
        var container = GetContainer();

        try
        {
            var items = await GetAllAsync(userId);

            var tasks = items.Select(item =>
                container.DeleteItemAsync<GroceryListItem>(item.Id, new PartitionKey(PartitionKeyValue)));

            await Task.WhenAll(tasks);

            _logger.LogInformation("Reset grocery list (deleted {Count} items) for user {UserId}.", items.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting grocery list for user {UserId}.", userId);
            throw;
        }
    }
}

