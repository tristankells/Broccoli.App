using Broccoli.Data.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// CosmosDB implementation of <see cref="IDailyFoodPlanService"/>.
/// Container: DailyFoodPlans, partition key: /userId.
/// Follows the same patterns as CosmosMealPrepPlanService.
/// </summary>
public class CosmosDailyFoodPlanService : IDailyFoodPlanService
{
    private readonly CosmosClient _cosmosClient;
    private readonly IAuthenticationStateService _authStateService;
    private readonly ILogger<CosmosDailyFoodPlanService> _logger;
    private Container? _container;
    private bool _initialized;

    private const string DatabaseId  = "BroccoliAppDb";
    private const string ContainerId = "DailyFoodPlans";

    public CosmosDailyFoodPlanService(
        CosmosClient cosmosClient,
        IAuthenticationStateService authStateService,
        ILogger<CosmosDailyFoodPlanService> logger)
    {
        _cosmosClient     = cosmosClient;
        _authStateService = authStateService;
        _logger           = logger;
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _logger.LogInformation("Initializing DailyFoodPlans container in CosmosDB...");

            var database = _cosmosClient.GetDatabase(DatabaseId);

            var response = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id               = ContainerId,
                    PartitionKeyPath = "/userId"
                });

            _container   = response.Container;
            _initialized = true;

            _logger.LogInformation("DailyFoodPlans container ready.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing DailyFoodPlans container.");
            throw;
        }
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void EnsureAuthenticated()
    {
        if (!_authStateService.IsAuthenticated)
            throw new UnauthorizedAccessException("User must be authenticated to access daily food plans.");
    }

    private async Task EnsureInitializedAsync()
    {
        EnsureAuthenticated();
        if (!_initialized) await InitializeAsync();
    }

    private string CurrentUserId =>
        _authStateService.CurrentUserId
        ?? throw new UnauthorizedAccessException("User ID not available.");

    private Container Container =>
        _container ?? throw new InvalidOperationException("CosmosDailyFoodPlanService is not initialized.");

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public async Task<List<DailyFoodPlan>> GetAllAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            var results = new List<DailyFoodPlan>();
            using var iterator = Container.GetItemQueryIterator<DailyFoodPlan>(query);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page);
            }

            results = results
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} daily food plans for user {UserId}.", results.Count, userId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily food plans for user {UserId}.", CurrentUserId);
            throw;
        }
    }

    public async Task<DailyFoodPlan?> GetByIdAsync(string planId)
    {
        await EnsureInitializedAsync();
        return await GetByIdInternalAsync(planId);
    }

    public async Task<DailyFoodPlan> AddAsync(DailyFoodPlan plan)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId     = CurrentUserId;
            plan.UserId    = userId;
            plan.Id        = Guid.NewGuid().ToString();
            plan.CreatedAt = DateTime.UtcNow;
            plan.UpdatedAt = null;

            var response = await Container.CreateItemAsync(plan, new PartitionKey(userId));

            _logger.LogInformation("Created daily food plan {PlanId} '{PlanName}' for user {UserId}.",
                plan.Id, plan.Name, userId);

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating daily food plan '{PlanName}' for user {UserId}.",
                plan.Name, CurrentUserId);
            throw;
        }
    }

    public async Task<DailyFoodPlan> UpdateAsync(DailyFoodPlan plan)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId   = CurrentUserId;
            var existing = await GetByIdInternalAsync(plan.Id);

            if (existing is null)
                throw new KeyNotFoundException($"Daily food plan {plan.Id} not found.");

            if (existing.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update plan {PlanId} owned by {OwnerId}.",
                    userId, plan.Id, existing.UserId);
                throw new UnauthorizedAccessException("You can only update your own daily food plans.");
            }

            plan.UserId    = userId;
            plan.UpdatedAt = DateTime.UtcNow;
            plan.CreatedAt = existing.CreatedAt;

            var response = await Container.ReplaceItemAsync(plan, plan.Id, new PartitionKey(userId));

            _logger.LogInformation("Updated daily food plan {PlanId} for user {UserId}.", plan.Id, userId);
            return response.Resource;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error updating daily food plan {PlanId}.", plan.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string planId)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId   = CurrentUserId;
            var existing = await GetByIdInternalAsync(planId);

            if (existing is null)
                throw new KeyNotFoundException($"Daily food plan {planId} not found.");

            if (existing.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete plan {PlanId} owned by {OwnerId}.",
                    userId, planId, existing.UserId);
                throw new UnauthorizedAccessException("You can only delete your own daily food plans.");
            }

            await Container.DeleteItemAsync<DailyFoodPlan>(planId, new PartitionKey(userId));

            _logger.LogInformation("Deleted daily food plan {PlanId} for user {UserId}.", planId, userId);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error deleting daily food plan {PlanId}.", planId);
            throw;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<DailyFoodPlan?> GetByIdInternalAsync(string planId)
    {
        try
        {
            var response = await Container.ReadItemAsync<DailyFoodPlan>(
                planId, new PartitionKey(CurrentUserId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}

