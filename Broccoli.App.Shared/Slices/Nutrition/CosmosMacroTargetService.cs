using Broccoli.Data.Models;
using Broccoli.App.Shared.Slices.Auth;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Slices.Nutrition;

/// <summary>
/// CosmosDB implementation of <see cref="IMacroTargetService"/>.
/// Uses two containers in the shared "BroccoliAppDb" database:
///   - MacroTargets          (partition key: /userId)
///   - MacroTargetSettings   (partition key: /userId)
/// </summary>
public class CosmosMacroTargetService : IMacroTargetService
{
    private readonly CosmosClient _cosmosClient;
    private readonly IAuthenticationStateService _authStateService;
    private readonly ILogger<CosmosMacroTargetService> _logger;

    private Container? _targetsContainer;
    private Container? _settingsContainer;
    private bool _initialized;

    private const string DatabaseId             = "BroccoliAppDb";
    private const string TargetsContainerId     = "MacroTargets";
    private const string SettingsContainerId    = "MacroTargetSettings";

    public CosmosMacroTargetService(
        CosmosClient cosmosClient,
        IAuthenticationStateService authStateService,
        ILogger<CosmosMacroTargetService> logger)
    {
        _cosmosClient     = cosmosClient;
        _authStateService = authStateService;
        _logger           = logger;
    }

    // -- Initialisation -------------------------------------------------------

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _logger.LogInformation("Initializing MacroTargets containers in CosmosDB...");

            var database = _cosmosClient.GetDatabase(DatabaseId);

            var targetsResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties { Id = TargetsContainerId, PartitionKeyPath = "/userId" });

            var settingsResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties { Id = SettingsContainerId, PartitionKeyPath = "/userId" });

            _targetsContainer  = targetsResponse.Container;
            _settingsContainer = settingsResponse.Container;
            _initialized = true;

            _logger.LogInformation("MacroTargets containers ready.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MacroTargets containers.");
            throw;
        }
    }

    // -- Helpers --------------------------------------------------------------

    private string CurrentUserId =>
        _authStateService.CurrentUserId
        ?? throw new UnauthorizedAccessException("User must be authenticated to access macro targets.");

    private Container Targets =>
        _targetsContainer ?? throw new InvalidOperationException("MacroTargetService not initialised.");

    private Container Settings =>
        _settingsContainer ?? throw new InvalidOperationException("MacroTargetService not initialised.");

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized) await InitializeAsync();
    }

    // -- MacroTarget CRUD -----------------------------------------------------

    public async Task<List<MacroTarget>> GetAllAsync(string userId)
    {
        await EnsureInitializedAsync();

        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt ASC")
                .WithParameter("@userId", userId);

            var results = new List<MacroTarget>();
            using var iterator = Targets.GetItemQueryIterator<MacroTarget>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) });

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page);
            }

            _logger.LogInformation("Retrieved {Count} macro targets for user {UserId}.", results.Count, userId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting macro targets for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<MacroTarget> AddAsync(MacroTarget target)
    {
        await EnsureInitializedAsync();

        try
        {
            target.Id        = Guid.NewGuid().ToString();
            target.UserId    = CurrentUserId;
            target.CreatedAt = DateTime.UtcNow;
            target.UpdatedAt = DateTime.UtcNow;

            var response = await Targets.CreateItemAsync(
                target,
                new PartitionKey(target.UserId));

            _logger.LogInformation("Added macro target '{Name}' for user {UserId}.", target.Name, target.UserId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding macro target for user {UserId}.", CurrentUserId);
            throw;
        }
    }

    public async Task<MacroTarget> UpdateAsync(MacroTarget target)
    {
        await EnsureInitializedAsync();

        try
        {
            target.UpdatedAt = DateTime.UtcNow;

            var response = await Targets.ReplaceItemAsync(
                target,
                target.Id,
                new PartitionKey(target.UserId));

            _logger.LogInformation("Updated macro target '{Name}' for user {UserId}.", target.Name, target.UserId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating macro target {Id} for user {UserId}.", target.Id, target.UserId);
            throw;
        }
    }

    public async Task DeleteAsync(string id, string userId)
    {
        await EnsureInitializedAsync();

        try
        {
            await Targets.DeleteItemAsync<MacroTarget>(id, new PartitionKey(userId));
            _logger.LogInformation("Deleted macro target {Id} for user {UserId}.", id, userId);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Macro target {Id} not found during delete for user {UserId}.", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting macro target {Id} for user {UserId}.", id, userId);
            throw;
        }
    }

    // -- Settings -------------------------------------------------------------

    public async Task<MacroTargetSettings> GetSettingsAsync(string userId)
    {
        await EnsureInitializedAsync();

        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            using var iterator = Settings.GetItemQueryIterator<MacroTargetSettings>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) });

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                var settings = page.FirstOrDefault();
                if (settings != null) return settings;
            }

            // No settings stored yet — return defaults without persisting
            _logger.LogInformation("No macro settings found for user {UserId}. Returning defaults.", userId);
            return new MacroTargetSettings { UserId = userId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting macro settings for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<MacroTargetSettings> SaveSettingsAsync(MacroTargetSettings settings)
    {
        await EnsureInitializedAsync();

        try
        {
            settings.UserId    = CurrentUserId;
            settings.UpdatedAt = DateTime.UtcNow;

            // Upsert: replaces if document exists, creates if not
            var response = await Settings.UpsertItemAsync(
                settings,
                new PartitionKey(settings.UserId));

            _logger.LogInformation("Saved macro settings for user {UserId}.", settings.UserId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving macro settings for user {UserId}.", settings.UserId);
            throw;
        }
    }
}

