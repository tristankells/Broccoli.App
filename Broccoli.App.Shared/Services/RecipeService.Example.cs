using Broccoli.App.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Example Recipe Service that is internally aware of the logged-in user.
/// This demonstrates how to integrate authentication into your services.
/// </summary>
public interface IRecipeService
{
    Task<IEnumerable<Recipe>> GetUserRecipesAsync();
    Task<Recipe?> GetRecipeByIdAsync(string recipeId);
    Task<Recipe> CreateRecipeAsync(Recipe recipe);
    Task<Recipe> UpdateRecipeAsync(Recipe recipe);
    Task DeleteRecipeAsync(string recipeId);
}

public class RecipeService : IRecipeService
{
    private readonly IAuthenticationStateService _authStateService;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(
        IAuthenticationStateService authStateService,
        ICosmosDbService cosmosDbService,
        ILogger<RecipeService> logger)
    {
        _authStateService = authStateService;
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    /// <summary>
    /// Gets recipes for the currently logged-in user.
    /// Automatically filters by current user ID.
    /// </summary>
    public async Task<IEnumerable<Recipe>> GetUserRecipesAsync()
    {
        EnsureAuthenticated();

        var userId = _authStateService.CurrentUserId!;
        _logger.LogInformation("Fetching recipes for user {UserId}", userId);

        // Your implementation here - this is just an example
        // You would query CosmosDB for recipes where userId matches
        return Enumerable.Empty<Recipe>();
    }

    /// <summary>
    /// Gets a specific recipe by ID, ensuring it belongs to the current user.
    /// </summary>
    public async Task<Recipe?> GetRecipeByIdAsync(string recipeId)
    {
        EnsureAuthenticated();

        var userId = _authStateService.CurrentUserId!;
        _logger.LogInformation("Fetching recipe {RecipeId} for user {UserId}", recipeId, userId);

        // Your implementation here
        // Query CosmosDB and verify the recipe belongs to the current user
        return null;
    }

    /// <summary>
    /// Creates a new recipe associated with the current user.
    /// Automatically sets the recipe's userId to the current user.
    /// </summary>
    public async Task<Recipe> CreateRecipeAsync(Recipe recipe)
    {
        EnsureAuthenticated();

        var userId = _authStateService.CurrentUserId!;
        var username = _authStateService.CurrentUsername!;

        // Automatically set the user information
        recipe.UserId = userId;
        recipe.CreatedBy = username;
        recipe.CreatedAt = DateTime.UtcNow;
        recipe.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Creating recipe '{RecipeName}' for user {UserId}", recipe.Name, userId);

        // Your implementation here
        // Save to CosmosDB
        return recipe;
    }

    /// <summary>
    /// Updates an existing recipe, ensuring it belongs to the current user.
    /// </summary>
    public async Task<Recipe> UpdateRecipeAsync(Recipe recipe)
    {
        EnsureAuthenticated();

        var userId = _authStateService.CurrentUserId!;

        // Verify ownership
        if (recipe.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update recipe {RecipeId} owned by {OwnerId}",
                userId, recipe.Id, recipe.UserId);
            throw new UnauthorizedAccessException("You can only update your own recipes.");
        }

        recipe.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Updating recipe '{RecipeName}' for user {UserId}", recipe.Name, userId);

        // Your implementation here
        // Update in CosmosDB
        return recipe;
    }

    /// <summary>
    /// Deletes a recipe, ensuring it belongs to the current user.
    /// </summary>
    public async Task DeleteRecipeAsync(string recipeId)
    {
        EnsureAuthenticated();

        var userId = _authStateService.CurrentUserId!;

        // First, get the recipe to verify ownership
        var recipe = await GetRecipeByIdAsync(recipeId);
        if (recipe == null)
        {
            throw new KeyNotFoundException($"Recipe {recipeId} not found.");
        }

        if (recipe.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete recipe {RecipeId} owned by {OwnerId}",
                userId, recipeId, recipe.UserId);
            throw new UnauthorizedAccessException("You can only delete your own recipes.");
        }

        _logger.LogInformation("Deleting recipe {RecipeId} for user {UserId}", recipeId, userId);

        // Your implementation here
        // Delete from CosmosDB
    }

    /// <summary>
    /// Ensures the user is authenticated before performing operations.
    /// </summary>
    private void EnsureAuthenticated()
    {
        if (!_authStateService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User must be authenticated to access recipes.");
        }
    }
}

/// <summary>
/// Example Recipe model
/// </summary>
public class Recipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string PartitionKey { get; set; } = "recipe";
}

// USAGE EXAMPLE IN A BLAZOR COMPONENT:
/*
@page "/recipes"
@using Broccoli.App.Shared.Services
@using Broccoli.App.Shared.Components
@inject IRecipeService RecipeService
@inject IAuthenticationStateService AuthState

<AuthorizeView>
    <h1>My Recipes</h1>
    <p>Logged in as: @AuthState.CurrentUsername</p>

    @if (recipes == null)
    {
        <p><em>Loading...</em></p>
    }
    else if (!recipes.Any())
    {
        <p>No recipes yet. Create your first recipe!</p>
    }
    else
    {
        <ul>
            @foreach (var recipe in recipes)
            {
                <li>@recipe.Name - Created by @recipe.CreatedBy</li>
            }
        </ul>
    }

    <button class="btn btn-primary" @onclick="CreateNewRecipe">Create Recipe</button>
</AuthorizeView>

@code {
    private IEnumerable<Recipe>? recipes;

    protected override async Task OnInitializedAsync()
    {
        // This automatically gets recipes for the currently logged-in user
        recipes = await RecipeService.GetUserRecipesAsync();
    }

    private async Task CreateNewRecipe()
    {
        var newRecipe = new Recipe
        {
            Name = "My New Recipe",
            Description = "A delicious recipe",
            Ingredients = new List<string> { "Ingredient 1", "Ingredient 2" },
            Instructions = new List<string> { "Step 1", "Step 2" }
            // UserId, CreatedBy, timestamps are automatically set by the service
        };

        var created = await RecipeService.CreateRecipeAsync(newRecipe);
        recipes = await RecipeService.GetUserRecipesAsync(); // Refresh
        StateHasChanged();
    }
}
*/

// SERVICE REGISTRATION:
/*
In MauiProgram.cs or Program.cs, add:

builder.Services.AddSingleton<IRecipeService, RecipeService>();

or for web (scoped per request):

builder.Services.AddScoped<IRecipeService, RecipeService>();
*/
