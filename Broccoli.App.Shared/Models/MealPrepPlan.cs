using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

/// <summary>
/// A named collection of recipes the user wants to prepare together.
/// Only recipe IDs are stored; full Recipe objects are joined in memory at render time.
/// </summary>
public class MealPrepPlan
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>User-chosen display name, e.g. "Week 1 – Bulking".</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ordered list of recipe IDs in this plan.
    /// Full Recipe objects are loaded separately and joined in memory.
    /// Missing IDs (deleted recipes) are silently skipped at render time.
    /// </summary>
    [JsonPropertyName("recipeIds")]
    public List<string> RecipeIds { get; set; } = new();

    /// <summary>ID of the owning user (partition key).</summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User-defined display order. Lower values appear first.
    /// Defaults to 0 so new plans sort to the top (tiebroken by CreatedAt DESC).
    /// Assigned as a clean 0-based integer by <see cref="IMealPrepPlanService.ReorderAsync"/>
    /// whenever the user drags to reorder.
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; } = 0;
}

