using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using UserModel = Broccoli.App.Shared.Models.User;

namespace Broccoli.App.Shared._Shared.Infrastructure;

public partial class CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger) : ICosmosDbService
{
    private readonly CosmosClient _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
    private readonly ILogger<CosmosDbService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private Container? _userContainer;
    private bool _initialized;

    private const string DatabaseId = "BroccoliAppDb";
    private const string UserContainerId = "Users";
    public const string FoodsContainerId = "Foods";

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Initializing CosmosDB...");

            // Create database with shared throughput if it doesn't exist
            // All containers in this database will share the 400 RU/s
            DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                DatabaseId,
                ThroughputProperties.CreateManualThroughput(600)); // Shared across all containers

            Database database = databaseResponse.Database;
            LogDatabaseReady(DatabaseId);

            // Create container for users (no throughput specified - uses shared)
            ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = UserContainerId,
                    PartitionKeyPath = "/partitionKey"
                });

            _userContainer = containerResponse.Container;
            _logger.LogInformation("Container {ContainerId} ready", UserContainerId);

            // Create container for foods (shared across all users)
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = FoodsContainerId,
                    PartitionKeyPath = "/partitionKey"
                });
            _logger.LogInformation("Container {ContainerId} ready", FoodsContainerId);

            _initialized = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error initializing CosmosDB");
            throw;
        }
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        await EnsureInitializedAsync();

        try
        {
            QueryDefinition query = new QueryDefinition(
                "SELECT * FROM c WHERE c.username = @username")
                .WithParameter("@username", username);

            using FeedIterator<UserModel> iterator = _userContainer!.GetItemQueryIterator<UserModel>(query);

            while (iterator.HasMoreResults)
            {
                FeedResponse<UserModel> response = await iterator.ReadNextAsync();
                UserModel? user = response.FirstOrDefault();
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }
        catch (CosmosException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error getting user by username {Username}", username);
            throw;
        }
    }

    public async Task<UserModel> CreateUserAsync(UserModel user)
    {
        await EnsureInitializedAsync();

        try
        {
            ItemResponse<UserModel> response = await _userContainer!.CreateItemAsync(
                user,
                new PartitionKey(user.PartitionKey));

            _logger.LogInformation("User {Username} created successfully", user.Username);
            return response.Resource;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error creating user {Username}", user.Username);
            throw;
        }
    }

    public async Task<UserModel> UpdateUserAsync(UserModel user)
    {
        await EnsureInitializedAsync();

        try
        {
            ItemResponse<UserModel> response = await _userContainer!.ReplaceItemAsync(
                user,
                user.Id,
                new PartitionKey(user.PartitionKey));

            _logger.LogInformation("User {Username} updated successfully", user.Username);
            return response.Resource;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error updating user {Username}", user.Username);
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Database {DatabaseId} ready with shared throughput")]
    private partial void LogDatabaseReady(string databaseId);
}
