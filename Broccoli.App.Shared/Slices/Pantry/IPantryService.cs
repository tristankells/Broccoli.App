using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Slices.Pantry;

/// <summary>
/// Service for managing the user's pantry inventory.
/// All operations are scoped to the supplied userId.
/// </summary>
public interface IPantryService
{
    /// <summary>
    /// Ensures the CosmosDB container exists. Must be called once at startup.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Returns all pantry items belonging to the specified user.
    /// </summary>
    Task<List<PantryItem>> GetAllAsync(string userId);

    /// <summary>
    /// Adds a new pantry item. UserId and timestamps are set automatically.
    /// </summary>
    Task<PantryItem> AddAsync(PantryItem item);

    /// <summary>
    /// Updates an existing pantry item (e.g. to change its category).
    /// </summary>
    Task<PantryItem> UpdateAsync(PantryItem item);

    /// <summary>
    /// Deletes a pantry item by id.
    /// </summary>
    Task DeleteAsync(string id, string userId);

    /// <summary>
    /// Returns true if a pantry item whose name contains <paramref name="itemName"/> (case-insensitive)
    /// already exists for the user.
    /// </summary>
    Task<bool> ExistsAsync(string userId, string itemName);
}

