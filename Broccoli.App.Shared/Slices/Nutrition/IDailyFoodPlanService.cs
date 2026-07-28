using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Slices.Nutrition;

/// <summary>
/// Manages daily food plans, scoped to the current authenticated user.
/// Each plan is a CosmosDB document with all tabs and rows embedded.
/// </summary>
public interface IDailyFoodPlanService
{
    /// <summary>Ensures the CosmosDB container exists. Must be called once at startup (web host).</summary>
    Task InitializeAsync();

    /// <summary>Returns all daily food plans for the current user, newest first.</summary>
    Task<List<DailyFoodPlan>> GetAllAsync();

    /// <summary>Returns a single plan by ID (ownership is verified). Returns null if not found.</summary>
    Task<DailyFoodPlan?> GetByIdAsync(string planId);

    /// <summary>Persists a new plan. UserId, Id and CreatedAt are set automatically.</summary>
    Task<DailyFoodPlan> AddAsync(DailyFoodPlan plan);

    /// <summary>Replaces an existing plan document. Verifies ownership and sets UpdatedAt.</summary>
    Task<DailyFoodPlan> UpdateAsync(DailyFoodPlan plan);

    /// <summary>Deletes a plan by ID after verifying ownership.</summary>
    Task DeleteAsync(string planId);
}

