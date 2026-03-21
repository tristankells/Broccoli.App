using Broccoli.Data.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// CosmosDB implementation of <see cref="IMealPrepPlanService"/>.
/// Container: MealPrepPlans, partition key: /userId.
/// Follows the same patterns as CosmosRecipeService and CosmosMacroTargetService.
/// </summary>
public class CosmosMealPrepPlanService : IMealPrepPlanService
{
    private readonly CosmosClient _cosmosClient;
    private readonly IAuthenticationStateService _authStateService;
    private readonly ILogger<CosmosMealPrepPlanService> _logger;
    private Container? _container;
    private bool _initialized;

    private const string DatabaseId   = "BroccoliAppDb";
    private const string ContainerId  = "MealPrepPlans";

    public CosmosMealPrepPlanService(
        CosmosClient cosmosClient,
        IAuthenticationStateService authStateService,
        ILogger<CosmosMealPrepPlanService> logger)
    {
        _cosmosClient     = cosmosClient;
        _authStateService = authStateService;
        _logger           = logger;
    }

    // ── Initialisation ───────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _logger.LogInformation("Initializing MealPrepPlans container in CosmosDB...");

            var database = _cosmosClient.GetDatabase(DatabaseId);

            var response = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id               = ContainerId,
                    PartitionKeyPath = "/userId"    // no dedicated RU/s — shares database throughput
                });

            _container   = response.Container;
            _initialized = true;

            _logger.LogInformation("MealPrepPlans container ready.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MealPrepPlans container.");
            throw;
        }
    }

    // ── Guards ───────────────────────────────────────────────────────────────

    private void EnsureAuthenticated()
    {
        if (!_authStateService.IsAuthenticated)
            throw new UnauthorizedAccessException("User must be authenticated to access meal prep plans.");
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
        _container ?? throw new InvalidOperationException("CosmosMealPrepPlanService is not initialised.");

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<List<MealPrepPlan>> GetAllAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;
            // Fetch without an ORDER BY so no composite index is required.
            // Sort in memory: explicit SortOrder first, then newest-first as tiebreaker
            // so plans created before the user has ever dragged still appear in a sensible order.
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            var results = new List<MealPrepPlan>();
            using var iterator = Container.GetItemQueryIterator<MealPrepPlan>(query);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page);
            }

            results = results
                .OrderBy(p => p.SortOrder)
                .ThenByDescending(p => p.CreatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} meal prep plans for user {UserId}.", results.Count, userId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meal prep plans for user {UserId}.", CurrentUserId);
            throw;
        }
    }

    public async Task ReorderAsync(List<string> orderedPlanIds)
    {
        await EnsureInitializedAsync();

        var userId  = CurrentUserId;
        var current = await GetAllAsync();
        var planById = current.ToDictionary(p => p.Id);

        var updateTasks = new List<Task>();

        for (int i = 0; i < orderedPlanIds.Count; i++)
        {
            if (!planById.TryGetValue(orderedPlanIds[i], out var plan)) continue;
            if (plan.UserId != userId) continue;
            if (plan.SortOrder == i)   continue; // already correct — skip the write

            plan.SortOrder = i;
            plan.UpdatedAt = DateTime.UtcNow;

            var captured = plan; // capture for the lambda
            updateTasks.Add(Container.ReplaceItemAsync(captured, captured.Id, new PartitionKey(userId)));
        }

        if (updateTasks.Count > 0)
        {
            await Task.WhenAll(updateTasks);
            _logger.LogInformation(
                "Persisted new order for {Count} plans for user {UserId}.", updateTasks.Count, userId);
        }
    }

    public async Task<MealPrepPlan> AddAsync(MealPrepPlan plan)
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

            _logger.LogInformation("Created meal prep plan {PlanId} '{PlanName}' for user {UserId}.",
                plan.Id, plan.Name, userId);

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meal prep plan '{PlanName}' for user {UserId}.",
                plan.Name, CurrentUserId);
            throw;
        }
    }

    public async Task<MealPrepPlan> UpdateAsync(MealPrepPlan plan)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId   = CurrentUserId;
            var existing = await GetByIdAsync(plan.Id);

            if (existing is null)
                throw new KeyNotFoundException($"Meal prep plan {plan.Id} not found.");

            if (existing.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update plan {PlanId} owned by {OwnerId}.",
                    userId, plan.Id, existing.UserId);
                throw new UnauthorizedAccessException("You can only update your own meal prep plans.");
            }

            plan.UserId    = userId;
            plan.UpdatedAt = DateTime.UtcNow;
            plan.CreatedAt = existing.CreatedAt;

            var response = await Container.ReplaceItemAsync(plan, plan.Id, new PartitionKey(userId));

            _logger.LogInformation("Updated meal prep plan {PlanId} for user {UserId}.", plan.Id, userId);
            return response.Resource;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error updating meal prep plan {PlanId}.", plan.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string planId)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId   = CurrentUserId;
            var existing = await GetByIdAsync(planId);

            if (existing is null)
                throw new KeyNotFoundException($"Meal prep plan {planId} not found.");

            if (existing.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete plan {PlanId} owned by {OwnerId}.",
                    userId, planId, existing.UserId);
                throw new UnauthorizedAccessException("You can only delete your own meal prep plans.");
            }

            await Container.DeleteItemAsync<MealPrepPlan>(planId, new PartitionKey(userId));

            _logger.LogInformation("Deleted meal prep plan {PlanId} for user {UserId}.", planId, userId);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error deleting meal prep plan {PlanId}.", planId);
            throw;
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<MealPrepPlan?> GetByIdAsync(string planId)
    {
        try
        {
            var response = await Container.ReadItemAsync<MealPrepPlan>(
                planId, new PartitionKey(CurrentUserId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}

