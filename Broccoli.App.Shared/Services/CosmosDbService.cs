using Broccoli.App.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using UserModel = Broccoli.App.Shared.Models.User;

namespace Broccoli.App.Shared.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<CosmosDbService> _logger;
    private Container? _userContainer;
    private bool _initialized;

    private const string DatabaseId = "BroccoliAppDb";
    private const string UserContainerId = "Users";

    public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            _logger.LogInformation("Initializing CosmosDB...");

            // Create database if it doesn't exist
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                DatabaseId,
                ThroughputProperties.CreateManualThroughput(400));

            var database = databaseResponse.Database;
            _logger.LogInformation("Database {DatabaseId} ready", DatabaseId);

            // Create container for users if it doesn't exist
            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = UserContainerId,
                    PartitionKeyPath = "/partitionKey"
                },
                ThroughputProperties.CreateManualThroughput(400));

            _userContainer = containerResponse.Container;
            _logger.LogInformation("Container {ContainerId} ready", UserContainerId);

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing CosmosDB");
            throw;
        }
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        await EnsureInitializedAsync();

        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.username = @username")
                .WithParameter("@username", username);

            using var iterator = _userContainer!.GetItemQueryIterator<UserModel>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var user = response.FirstOrDefault();
                if (user != null)
                    return user;
            }

            return null;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username {Username}", username);
            throw;
        }
    }

    public async Task<UserModel> CreateUserAsync(UserModel user)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _userContainer!.CreateItemAsync(
                user,
                new PartitionKey(user.PartitionKey));

            _logger.LogInformation("User {Username} created successfully", user.Username);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", user.Username);
            throw;
        }
    }

    public async Task<UserModel> UpdateUserAsync(UserModel user)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _userContainer!.ReplaceItemAsync(
                user,
                user.Id,
                new PartitionKey(user.PartitionKey));

            _logger.LogInformation("User {Username} updated successfully", user.Username);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Username}", user.Username);
            throw;
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }
    }
}
