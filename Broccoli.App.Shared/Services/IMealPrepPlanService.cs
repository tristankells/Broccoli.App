using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Manages meal prep plans, scoped to the current authenticated user.
/// </summary>
public interface IMealPrepPlanService
{
    /// <summary>Ensures the CosmosDB container exists. Must be called once at startup (web host).</summary>
    Task InitializeAsync();

    /// <summary>Returns all plans for the current user, newest first.</summary>
    Task<List<MealPrepPlan>> GetAllAsync();

    /// <summary>Persists a new plan. UserId, Id and CreatedAt are set automatically.</summary>
    Task<MealPrepPlan> AddAsync(MealPrepPlan plan);

    /// <summary>Replaces an existing plan document. Verifies ownership and sets UpdatedAt.</summary>
    Task<MealPrepPlan> UpdateAsync(MealPrepPlan plan);

    /// <summary>Deletes a plan by ID after verifying ownership.</summary>
    Task DeleteAsync(string planId);

    /// <summary>
    /// Persists a new display order for the current user's plans.
    /// <paramref name="orderedPlanIds"/> is the complete list of plan IDs in the desired order.
    /// Only plans whose SortOrder has actually changed are written to CosmosDB.
    /// </summary>
    Task ReorderAsync(List<string> orderedPlanIds);
}

