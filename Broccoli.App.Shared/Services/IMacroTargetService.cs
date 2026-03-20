using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Service for managing macro / calorie targets and their per-user settings.
/// All target operations are automatically scoped to the current authenticated user.
/// </summary>
public interface IMacroTargetService
{
    /// <summary>Ensures CosmosDB containers exist. Must be called once at startup.</summary>
    Task InitializeAsync();

    /// <summary>Returns all macro targets belonging to the specified user, ordered by creation date.</summary>
    Task<List<MacroTarget>> GetAllAsync(string userId);

    /// <summary>Adds a new macro target. UserId and timestamps are set automatically.</summary>
    Task<MacroTarget> AddAsync(MacroTarget target);

    /// <summary>Replaces an existing macro target document. UpdatedAt is set automatically.</summary>
    Task<MacroTarget> UpdateAsync(MacroTarget target);

    /// <summary>Deletes a macro target by id.</summary>
    Task DeleteAsync(string id, string userId);

    /// <summary>
    /// Returns the settings document for the given user.
    /// If none exists yet, returns a new <see cref="MacroTargetSettings"/> with all defaults
    /// (does NOT persist until <see cref="SaveSettingsAsync"/> is called).
    /// </summary>
    Task<MacroTargetSettings> GetSettingsAsync(string userId);

    /// <summary>Upserts the settings document for the user.</summary>
    Task<MacroTargetSettings> SaveSettingsAsync(MacroTargetSettings settings);
}

