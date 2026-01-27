using Broccoli.Data.Models;

namespace Broccoli.Shared.Services;

/// <summary>
/// Service for managing recipes with user-scoped access.
/// All operations are automatically filtered to the current authenticated user.
/// </summary>
public interface IRecipeService
{
    /// <summary>
    /// Gets all recipes belonging to the current authenticated user.
    /// </summary>
    /// <returns>Collection of user's recipes</returns>
    Task<IEnumerable<Recipe>> GetAllAsync();

    /// <summary>
    /// Gets a specific recipe by ID, ensuring it belongs to the current user.
    /// </summary>
    /// <param name="recipeId">The recipe ID</param>
    /// <returns>The recipe if found and belongs to user, null otherwise</returns>
    Task<Recipe?> GetByIdAsync(string recipeId);

    /// <summary>
    /// Adds a new recipe for the current authenticated user.
    /// UserId and timestamps are automatically set.
    /// </summary>
    /// <param name="recipe">The recipe to add</param>
    /// <returns>The created recipe with generated ID</returns>
    Task<Recipe> AddAsync(Recipe recipe);

    /// <summary>
    /// Updates an existing recipe, ensuring it belongs to the current user.
    /// UpdatedAt timestamp is automatically set.
    /// </summary>
    /// <param name="recipe">The recipe to update</param>
    /// <returns>The updated recipe</returns>
    /// <exception cref="UnauthorizedAccessException">If recipe doesn't belong to current user</exception>
    /// <exception cref="KeyNotFoundException">If recipe doesn't exist</exception>
    Task<Recipe> UpdateAsync(Recipe recipe);

    /// <summary>
    /// Deletes a recipe, ensuring it belongs to the current user.
    /// </summary>
    /// <param name="recipeId">The recipe ID to delete</param>
    /// <exception cref="UnauthorizedAccessException">If recipe doesn't belong to current user</exception>
    /// <exception cref="KeyNotFoundException">If recipe doesn't exist</exception>
    Task DeleteAsync(string recipeId);

    /// <summary>
    /// Searches recipes by name for the current user.
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>Matching recipes</returns>
    Task<IEnumerable<Recipe>> SearchByNameAsync(string searchTerm);

    /// <summary>
    /// Gets recipes filtered by tags for the current user.
    /// </summary>
    /// <param name="tags">Tags to filter by</param>
    /// <returns>Recipes matching any of the tags</returns>
    Task<IEnumerable<Recipe>> GetByTagsAsync(IEnumerable<string> tags);
}
