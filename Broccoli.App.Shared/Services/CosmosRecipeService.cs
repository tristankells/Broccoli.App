using Broccoli.Data.Models;
using Broccoli.App.Shared.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Broccoli.Shared.Services;

/// <summary>
/// CosmosDB implementation of IRecipeService with automatic user-scoping.
/// All operations are filtered to the current authenticated user.
/// </summary>
public class CosmosRecipeService : IRecipeService
{
    private readonly CosmosClient _cosmosClient;
    private readonly IAuthenticationStateService _authStateService;
    private readonly ILogger<CosmosRecipeService> _logger;
    private Container? _recipesContainer;
    private bool _initialized;

    private const string DatabaseId = "BroccoliAppDb";
    private const string RecipesContainerId = "Recipes";

    public CosmosRecipeService(
        CosmosClient cosmosClient,
        IAuthenticationStateService authStateService,
        ILogger<CosmosRecipeService> logger)
    {
        _cosmosClient = cosmosClient;
        _authStateService = authStateService;
        _logger = logger;
    }

    /// <summary>
    /// Ensures CosmosDB is initialized and user is authenticated
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        EnsureAuthenticated();

        if (_initialized)
            return;

        try
        {
            _logger.LogInformation("Initializing Recipes container in CosmosDB...");

            var database = _cosmosClient.GetDatabase(DatabaseId);

            // Create Recipes container without dedicated throughput (uses shared database throughput)
            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = RecipesContainerId,
                    PartitionKeyPath = "/userId" // Partition by userId for optimal performance
                });

            _recipesContainer = containerResponse.Container;
            _logger.LogInformation("Recipes container ready (using shared throughput)");

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Recipes container");
            throw;
        }
    }

    /// <summary>
    /// Ensures user is authenticated before any operation
    /// </summary>
    private void EnsureAuthenticated()
    {
        if (!_authStateService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User must be authenticated to access recipes.");
        }
    }

    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    private string CurrentUserId => _authStateService.CurrentUserId 
        ?? throw new UnauthorizedAccessException("User ID not available");

    public async Task<IEnumerable<Recipe>> GetAllAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt DESC")
                .WithParameter("@userId", userId);

            var results = new List<Recipe>();
            using var iterator = _recipesContainer!.GetItemQueryIterator<Recipe>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Retrieved {Count} recipes for user {UserId}", results.Count, userId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipes for user {UserId}", CurrentUserId);
            throw;
        }
    }

    public async Task<Recipe?> GetByIdAsync(string recipeId)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;
            var response = await _recipesContainer!.ReadItemAsync<Recipe>(
                recipeId,
                new PartitionKey(userId));

            _logger.LogInformation("Retrieved recipe {RecipeId} for user {UserId}", recipeId, userId);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Recipe {RecipeId} not found for user {UserId}", recipeId, CurrentUserId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe {RecipeId} for user {UserId}", recipeId, CurrentUserId);
            throw;
        }
    }

    public async Task<Recipe> AddAsync(Recipe recipe)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;

            // Automatically set user context
            recipe.UserId = userId;
            recipe.Id = Guid.NewGuid().ToString();
            recipe.CreatedAt = DateTime.UtcNow;

            var response = await _recipesContainer!.CreateItemAsync(
                recipe,
                new PartitionKey(userId));

            _logger.LogInformation("Created recipe {RecipeId} '{RecipeName}' for user {UserId}", 
                recipe.Id, recipe.Name, userId);

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recipe '{RecipeName}' for user {UserId}", 
                recipe.Name, CurrentUserId);
            throw;
        }
    }

    public async Task<Recipe> UpdateAsync(Recipe recipe)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;

            // Verify ownership
            var existing = await GetByIdAsync(recipe.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Recipe {recipe.Id} not found.");
            }

            if (existing.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to update recipe {RecipeId} owned by {OwnerId}",
                    userId, recipe.Id, existing.UserId);
                throw new UnauthorizedAccessException("You can only update your own recipes.");
            }

            // Ensure userId cannot be changed and update timestamp
            recipe.UserId = userId;
            recipe.UpdatedAt = DateTime.UtcNow;
            recipe.CreatedAt = existing.CreatedAt; // Preserve original creation time

            var response = await _recipesContainer!.ReplaceItemAsync(
                recipe,
                recipe.Id.ToString(),
                new PartitionKey(userId));

            _logger.LogInformation("Updated recipe {RecipeId} '{RecipeName}' for user {UserId}", 
                recipe.Id, recipe.Name, userId);

            return response.Resource;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error updating recipe {RecipeId} for user {UserId}", 
                recipe.Id, CurrentUserId);
            throw;
        }
    }

    public async Task DeleteAsync(string recipeId)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;

            // Verify ownership before deletion
            var existing = await GetByIdAsync(recipeId);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Recipe {recipeId} not found.");
            }

            if (existing.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete recipe {RecipeId} owned by {OwnerId}",
                    userId, recipeId, existing.UserId);
                throw new UnauthorizedAccessException("You can only delete your own recipes.");
            }

            await _recipesContainer!.DeleteItemAsync<Recipe>(
                recipeId,
                new PartitionKey(userId));

            _logger.LogInformation("Deleted recipe {RecipeId} for user {UserId}", recipeId, userId);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Error deleting recipe {RecipeId} for user {UserId}", 
                recipeId, CurrentUserId);
            throw;
        }
    }

    public async Task<IEnumerable<Recipe>> SearchByNameAsync(string searchTerm)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync();
            }

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId AND CONTAINS(LOWER(c.name), @searchTerm) ORDER BY c.createdAt DESC")
                .WithParameter("@userId", userId)
                .WithParameter("@searchTerm", searchTerm.ToLower());

            var results = new List<Recipe>();
            using var iterator = _recipesContainer!.GetItemQueryIterator<Recipe>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Found {Count} recipes matching '{SearchTerm}' for user {UserId}", 
                results.Count, searchTerm, userId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching recipes for term '{SearchTerm}' for user {UserId}", 
                searchTerm, CurrentUserId);
            throw;
        }
    }

    public async Task<IEnumerable<Recipe>> GetByTagsAsync(IEnumerable<string> tags)
    {
        await EnsureInitializedAsync();

        try
        {
            var userId = CurrentUserId;
            var tagsList = tags.ToList();

            if (!tagsList.Any())
            {
                return await GetAllAsync();
            }

            // Get all user recipes and filter in memory for tag matching
            // CosmosDB doesn't support ARRAY_CONTAINS with OR logic easily
            var allRecipes = await GetAllAsync();
            var filteredRecipes = allRecipes
                .Where(r => r.Tags.Any(t => tagsList.Contains(t, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            _logger.LogInformation("Found {Count} recipes with tags [{Tags}] for user {UserId}", 
                filteredRecipes.Count, string.Join(", ", tagsList), userId);

            return filteredRecipes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipes by tags for user {UserId}", CurrentUserId);
            throw;
        }
    }
}
