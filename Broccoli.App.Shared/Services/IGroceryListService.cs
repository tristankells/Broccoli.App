using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Service for managing the user's grocery list.
/// All operations are scoped to the supplied userId.
/// </summary>
public interface IGroceryListService
{
    /// <summary>
    /// Ensures the CosmosDB container exists. Must be called once at startup.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Returns all grocery list items belonging to the specified user, newest first.
    /// </summary>
    Task<List<GroceryListItem>> GetAllAsync(string userId);

    /// <summary>
    /// Adds a single grocery list item. UserId and timestamps are set automatically.
    /// </summary>
    Task<GroceryListItem> AddAsync(GroceryListItem item);

    /// <summary>
    /// Adds multiple grocery list items in one call (used when adding recipe ingredients).
    /// </summary>
    Task AddMultipleAsync(List<GroceryListItem> items);

    /// <summary>
    /// Updates an existing grocery list item (e.g. to toggle IsChecked).
    /// </summary>
    Task<GroceryListItem> UpdateAsync(GroceryListItem item);

    /// <summary>
    /// Deletes a single grocery list item by id.
    /// </summary>
    Task DeleteAsync(string id, string userId);

    /// <summary>
    /// Deletes ALL grocery list items for the specified user (the "Reset" action).
    /// </summary>
    Task ResetAsync(string userId);
}

